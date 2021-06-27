using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using agora_utilities;

public class AgoraScreenShare : MonoBehaviour 
{
    private string APP_ID = "95f80633644649e091a5c9035338683e";

    private static string channelToken = "";
    private static string tokenBase = "https://token-server-node.herokuapp.com";

    private CONNECTION_STATE_TYPE state = CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED;

    [SerializeField]
   	public Text logText;
    private Logger logger;
	public IRtcEngine mRtcEngine = null;
	private static string channelName = "881";
	private const float Offset = 100;
    //private Texture2D mTexture;
    public Texture2D mTexture;
    private Rect mRect;	
	private int i = 0;
    private WebCamTexture webCameraTexture;
    public RawImage rawImage;
	public Vector2 cameraSize = new Vector2(640, 480);
	public int cameraFPS = 15;

    public RenderTexture renderTexture;

	// Use this for initialization
	void Start () 
	{
        //InitCameraDevice();
        InitTexture();
		CheckAppId();	
		InitEngine();
        setupVideoEncodingConfig();
        JoinChannel();
        //TestRectCrop(0);
    }

    void Update() 
    {
        PermissionHelper.RequestMicrophontPermission();
		StartCoroutine(shareScreen());
    }

    IEnumerator shareScreen()
    {
        yield return new WaitForEndOfFrame();
        IRtcEngine rtc = IRtcEngine.QueryEngine();
        if (rtc != null)
        {
            mTexture.ReadPixels(mRect, 0, 0);
            mTexture.Apply();
            byte[] bytes = mTexture.GetRawTextureData();
            int size = Marshal.SizeOf(bytes[0]) * bytes.Length;
            ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
            externalVideoFrame.type = ExternalVideoFrame.VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
            externalVideoFrame.format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA;
            externalVideoFrame.buffer = bytes;
            externalVideoFrame.stride = (int)mRect.width;
            externalVideoFrame.height = (int)mRect.height;
            externalVideoFrame.cropLeft = 10;
            externalVideoFrame.cropTop = 10;
            externalVideoFrame.cropRight = 10;
            externalVideoFrame.cropBottom = 10;
            externalVideoFrame.rotation = 0;
            externalVideoFrame.timestamp = i++;
            int a = rtc.PushVideoFrame(externalVideoFrame);
            Debug.Log("PushVideoFrame ret = " + a);
        }
    }

    void TestRectCrop(int order)
    {
        ScreenCaptureParameters sparams = new ScreenCaptureParameters
        {
            captureMouseCursor = true,
            frameRate = 60,
            bitrate = 6300
        };


        mRtcEngine.StopScreenCapture();
        // Assuming you have two display monitors, each of 1920x1080, position left to right:
        Rectangle screenRect = new Rectangle() { x = 0, y = 0, width = 1920 * 2, height = 1080 };
        Rectangle regionRect = new Rectangle() { x = 1 * 1920, y = 0, width = 1920, height = 1080 };

        int rc = mRtcEngine.StartScreenCaptureByScreenRect(screenRect,
            regionRect,
            sparams
            );
        Debug.Log("start screen capture result " + rc);
        if (rc != 0) Debug.LogWarning("rc = " + rc);
    }

    void InitEngine()
	{
        mRtcEngine = IRtcEngine.GetEngine(APP_ID);
		mRtcEngine.SetLogFile("log.txt");
        mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        mRtcEngine.EnableAudio();
		mRtcEngine.EnableVideo();
		mRtcEngine.EnableVideoObserver();
		mRtcEngine.SetExternalVideoSource(true, false);
        mRtcEngine.OnJoinChannelSuccess += OnJoinChannelSuccessHandler;
        mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        mRtcEngine.OnWarning += OnSDKWarningHandler;
        mRtcEngine.OnError += OnSDKErrorHandler;
        mRtcEngine.OnConnectionLost += OnConnectionLostHandler;
        mRtcEngine.OnUserJoined += OnUserJoinedHandler;
        mRtcEngine.OnUserOffline += OnUserOfflineHandler;
	}

    void setupVideoEncodingConfig() {
        // Create a VideoEncoderConfiguration instance. See the descriptions of the parameters in API Reference.
        VideoEncoderConfiguration config = new VideoEncoderConfiguration();
        // Sets the video resolution.
        config.dimensions.width = 1280;
        config.dimensions.height = 720;
        // Sets the video frame rate.
        config.frameRate = FRAME_RATE.FRAME_RATE_FPS_30;
        // Sets the video encoding bitrate (Kbps).
        config.bitrate = 3420;
        // Sets the adaptive orientation mode. See the description in API Reference.
        config.orientationMode = ORIENTATION_MODE.ORIENTATION_MODE_ADAPTIVE;
        // Sets the video encoding degradation preference under limited bandwidth. MIANTAIN_QUALITY means to degrade the frame rate to maintain the video quality.
        config.degradationPreference = DEGRADATION_PREFERENCE.MAINTAIN_QUALITY;
        // Sets the video encoder configuration.
        mRtcEngine.SetVideoEncoderConfiguration(config);
    }

    void JoinChannel()
    {
        if (channelToken.Length == 0)
        {
            StartCoroutine(HelperClass.FetchToken(tokenBase, channelName, 0, this.RenewOrJoinToken));
            return;
        }
        mRtcEngine.JoinChannelByKey(channelToken, channelName, "", 0);
    }

    void RenewOrJoinToken(string newToken)
    {
        AgoraScreenShare.channelToken = newToken;
        if (state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
            || state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
            || state == CONNECTION_STATE_TYPE.CONNECTION_STATE_FAILED
        )
        {
            // If we are not connected yet, connect to the channel as normal
            JoinChannel();
        }
        else
        {
            // If we are already connected, we should just update the token
            UpdateToken();
        }
    }

    void UpdateToken()
    {
        mRtcEngine.RenewToken(AgoraScreenShare.channelToken);
    }

    void CheckAppId()
    {
        logger = new Logger(logText);
        logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in Canvas!!!!");
    }

    void InitTexture()
    {
        mRect = new Rect(0, 0, Screen.width, Screen.height);
        //mRect = GameObject.Find("outputImage").GetComponent<RectTransform>().rect;
        mTexture = new Texture2D((int)mRect.width, (int)mRect.height, TextureFormat.RGBA32, false);
    }

    public void InitCameraDevice()
    {   

        WebCamDevice[] devices = WebCamTexture.devices;
        webCameraTexture = new WebCamTexture(devices[0].name, (int)cameraSize.x, (int)cameraSize.y, cameraFPS);
        rawImage.texture = webCameraTexture;
        webCameraTexture.Play();
    }
	
	void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("onJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}", channelName, uid, elapsed));
    }

    void OnLeaveChannelHandler(RtcStats stats)
    {
        logger.UpdateLog("OnLeaveChannelSuccess");
    }

    void OnUserJoinedHandler(uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
        makeVideoView(uid);
    }

    void OnUserOfflineHandler(uint uid, USER_OFFLINE_REASON reason)
    {
        logger.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
        DestroyVideoView(uid);
    }

    void OnSDKWarningHandler(int warn, string msg)
    {
        logger.UpdateLog(string.Format("OnSDKWarning warn: {0}, msg: {1}", warn, msg));
    }
    
    void OnSDKErrorHandler(int error, string msg)
    {
        logger.UpdateLog(string.Format("OnSDKError error: {0}, msg: {1}", error, msg));
    }
    
    void OnConnectionLostHandler()
    {
        logger.UpdateLog(string.Format("OnConnectionLost "));
    }

    void OnApplicationQuit()
    {
        if (webCameraTexture)
        {
            webCameraTexture.Stop();
        }

        if (mRtcEngine != null)
        {
			mRtcEngine.LeaveChannel();
			mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
            mRtcEngine = null;
        }
    }

    private void DestroyVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            Destroy(go);
        }
    }

    private void makeVideoView(uint uid)
    {
        // GameObject go = GameObject.Find(uid.ToString());
        //if (!ReferenceEquals(go, null))
        // {
        //     return; // reuse
        // }

        // create a GameObject and assign to this new user
        //VideoSurface videoSurface = makeImageSurface(uid.ToString());
        VideoSurface videoSurface = makePlaneSurface(uid.ToString());
        //VideoSurface videoSurface = gameObject.AddComponent<VideoSurface>();
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.Renderer);
            videoSurface.SetGameFps(30);
        }
    }

    // VIDEO TYPE 1: 3D Object
    public VideoSurface makePlaneSurface(string goName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (go == null)
        {
            return null;
        }
        go.name = goName;
        // set up transform
        go.transform.Rotate(-90.0f, 0.0f, 0.0f);
        float yPos = Random.Range(3.0f, 5.0f);
        float xPos = Random.Range(-2.0f, 2.0f);
        go.transform.position = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    // Video TYPE 2: RawImage
    public VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // to be renderered onto
        go.AddComponent<RawImage>();
        //go.GetComponent<RawImage>().texture = renderTexture;
        // make the object draggable
        go.AddComponent<UIElementDrag>();
        GameObject canvas = GameObject.Find("VideoCanvas");
        if (canvas != null)
        {
            go.transform.parent = canvas.transform;
            Debug.Log("add video view");
        }
        else
        {
            Debug.Log("Canvas is null video view");
        }
        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
        Debug.Log("position x " + xPos + " y: " + yPos);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(3f, 4f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
}
