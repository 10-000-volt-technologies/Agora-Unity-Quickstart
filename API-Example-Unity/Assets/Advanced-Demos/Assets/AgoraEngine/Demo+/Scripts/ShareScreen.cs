using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;
using System.Globalization;
using System.Runtime.InteropServices;
using System;

public class ShareScreen : MonoBehaviour
{
    Texture2D mTexture;
    Rect mRect;
    [SerializeField]
    private string appId = "95f80633644649e091a5c9035338683e";
    [SerializeField]
    private string channelName = "test";
    public IRtcEngine mRtcEngine;
    int i = 100;

    void Start()
    {
        Debug.Log("ScreenShare Activated");
        mRtcEngine = IRtcEngine.GetEngine(appId);
        // Sets the output log level of the SDK.
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
        // Enables the video module.
        mRtcEngine.EnableVideo();
        // Enables the video observer.
        mRtcEngine.EnableVideoObserver();
        // Configures the external video source.
        mRtcEngine.SetExternalVideoSource(true, false);
        // Joins a channel.
        mRtcEngine.JoinChannel(channelName, null, 0);
        // Creates a rectangular region of the screen.
        mRect = new Rect(0, 0, Screen.width, Screen.height);
        // Creates a texture of the rectangle you create.
        mTexture = new Texture2D((int)mRect.width, (int)mRect.height, TextureFormat.RGBA32, false);

    }

    void Update()
    {
        StartCoroutine(shareScreen());
    }

    // Starts to share the screen.
    IEnumerator shareScreen()
    {
        yield return new WaitForEndOfFrame();
        GameObject.Find("DisplayPlane").GetComponent<RawImage>().texture = mTexture;
        // Reads the pixels of the rectangle you create.
        mTexture.ReadPixels(mRect, 0, 0);
        // Applies the pixels read from the rectangle to the texture.
        mTexture.Apply();
        // Gets the raw texture data and apply it to an array of bytes.
        byte[] bytes = mTexture.GetRawTextureData();
        // Gives enough space for the bytes array.
        int size = Marshal.SizeOf(bytes[0]) * bytes.Length;
        // Checks whether the IRtcEngine instance exists.
        IRtcEngine rtc = IRtcEngine.QueryEngine();
        if (rtc != null)
        {
            // Creates a new external video frame.
            ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
            // Sets the buffer type of the video frame.
            externalVideoFrame.type = ExternalVideoFrame.VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
            // Sets the format of the video pixel.
            externalVideoFrame.format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA;
            // Applies the raw data.
            externalVideoFrame.buffer = bytes;
            // Sets the width (pixel) of the video frame.
            externalVideoFrame.stride = (int)mRect.width;
            // Sets the height (pixel) of the video frame.
            externalVideoFrame.height = (int)mRect.height;
            // Removes pixels from the sides of the frame
            externalVideoFrame.cropLeft = 10;
            externalVideoFrame.cropTop = 10;
            externalVideoFrame.cropRight = 10;
            externalVideoFrame.cropBottom = 10;
            // Rotates the video frame (0, 90, 180, or 270)
            externalVideoFrame.rotation = 180;
            // Increments i with the video timestamp.
            externalVideoFrame.timestamp = i++;
            // Pushes the external video frame with the frame you create.
            int a = rtc.PushVideoFrame(externalVideoFrame);
        }
    }
}