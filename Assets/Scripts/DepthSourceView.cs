using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Threading;
using System;

public enum DepthViewMode
{
    SeparateSourceReaders,
    MultiSourceReader,
}

public class DepthSourceView : MonoBehaviour
{
    public DepthViewMode ViewMode = DepthViewMode.SeparateSourceReaders;

    public GameObject ColorSourceManager;
    public GameObject DepthSourceManager;
    public GameObject MultiSourceManager;
    public BodySourceView bodyView;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;

    // Only works at 4 right now
    private const int _DownsampleSize = 4;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;

    private MultiSourceManager _MultiManager;
    private ColorSourceManager _ColorManager;
    private DepthSourceManager _DepthManager;
    private Vector3 initialPos;
    private FrameDescription frameDesc;
    int frameWidth, frameHeight;
    private Thread thread = null;
    private bool running = true;
    Matrix4x4 localToWorld;
    void Start()
    {
        localToWorld = transform.localToWorldMatrix;
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
        _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();
        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();

        thread = new Thread(new ThreadStart(dataStream));
        thread.Start();
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }

    void OnGUI()
    {
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.TextField(new Rect(Screen.width - 250, 10, 250, 20), "DepthMode: " + ViewMode.ToString());
        GUI.EndGroup();
    }

    void Update()
    {
        localToWorld = transform.localToWorldMatrix;
        gameObject.GetComponent<Renderer>().material.mainTexture = _ColorManager.GetColorTexture();
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        //_Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
        _Mesh.RecalculateBounds();
    }

    private void dataStream()
    {
        while (running)
        {
            try
            {
                getKinectData();
            }
            catch (Exception e)
            {

                thread.Abort();
            }
        }


    }

    void OnDestroyed()
    {
        thread.Abort();
    }

    void OnApplicationQuit()
    {
        running = false;
        thread.Abort();
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
        if (_Mapper != null)
        {
            _Mapper = null;
        }
    }

    private void getKinectData()
    {
        if (_Sensor == null)
        {
            return;
        }

        if (ViewMode == DepthViewMode.SeparateSourceReaders)
        {
            if (ColorSourceManager == null)
            {
                return;
            }

            if (_ColorManager == null)
            {
                return;
            }

            if (DepthSourceManager == null)
            {
                return;
            }

            if (_DepthManager == null)
            {
                return;
            }
            if (_DepthManager.GetData() != null)
            {
                RefreshData(_DepthManager.GetData(),
                    _ColorManager.ColorWidth,
                    _ColorManager.ColorHeight);
            }

        }

    }

    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        if (frameDesc == null)
        {
            frameDesc = _Sensor.DepthFrameSource.FrameDescription;
            frameHeight = frameDesc.Height;
            frameWidth = frameDesc.Width;
        }

        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);
        for (int y = 0; y < frameHeight; y += _DownsampleSize)
        {
            for (int x = 0; x < frameWidth; x += _DownsampleSize)
            {
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int smallIndex = (indexY * (frameWidth / _DownsampleSize)) + indexX;

                double avg = GetAvg(depthData, x, y, frameWidth, frameHeight);

                avg = avg * _DepthScale;


                _Vertices[smallIndex].z = (float)avg;

                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[(y * frameWidth) + x];


                _UV[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);

            }
        }


    }

    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;

        for (int y1 = y; y1 < y + 4; y1++)
        {
            for (int x1 = x; x1 < x + 4; x1++)
            {
                int fullIndex = (y1 * width) + x1;

                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];

            }
        }

        return sum / 16;
    }


}
