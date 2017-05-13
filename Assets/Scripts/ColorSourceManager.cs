using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Threading;

public class ColorSourceManager : MonoBehaviour
{
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    private KinectSensor _Sensor;
    private ColorFrameReader _Reader;
    private Texture2D _Texture;
    private byte[] _Data;
    private byte[] keptFrame;
    private Thread thread = null;

    public Texture2D GetColorTexture()
    {
        return _Texture;
    }

    public void pause(bool stop)
    {
        if (stop)
        {
            thread.Abort();
        }
        else
        {
            thread = new Thread(new ThreadStart(updateImage));
            thread.Start();
        }

    }
    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.ColorFrameSource.OpenReader();

            var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;

            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
            keptFrame = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
        thread = new Thread(new ThreadStart(updateImage));
        thread.Start();
        //StartCoroutine(updateTexture());
    }

    private IEnumerator updateTexture()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.05f);
            _Texture.LoadRawTextureData(_Data);
            _Texture.Apply();
        }
    }
    void Update()
    {
        _Texture.LoadRawTextureData(_Data);
        _Texture.Apply();

    }

    void updateImage()
    {
        while (true)
        {
            try
            {
                if (_Reader != null)
                {
                    var frame = _Reader.AcquireLatestFrame();

                    if (frame != null)
                    {
                        frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                        //System.Array.Copy(_Data, keptFrame, _Data.Length);
                        frame.Dispose();
                        frame = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Thread encountered an error");
                Thread.CurrentThread.Abort();
            }
            Thread.Sleep(3);
        }
    }

    void OnDestroyed()
    {
        thread.Abort();
    }

    void OnApplicationQuit()
    {
        thread.Abort();
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
