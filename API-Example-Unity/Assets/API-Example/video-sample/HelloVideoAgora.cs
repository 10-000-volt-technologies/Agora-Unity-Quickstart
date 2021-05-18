using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;
using System.Collections.Concurrent;
using System;

public class HelloVideoAgora : MonoBehaviour
{
	
    [SerializeField]
    private string APP_ID = "999988d7c6884c2c83415838c43674dc";

    [SerializeField]
    private string TOKEN = "006999988d7c6884c2c83415838c43674dcIAC79dURK/4pYdCg/eWxQeafLkVzvqNdgKHxntyKJnxg04H/KYQAAAAAEAB33sfLdZmbYAEAAQBFmJtg";

    [SerializeField]
    private string CHANNEL_NAME = "YOUR_CHANNEL_NAME";
    public Text logText;
	private Logger logger;
	private IRtcEngine mRtcEngine = null;
    private const float Offset = 100;
    private static string channelName = "Agora_Channel";
    AudioClip clip;
    AudioSource audioSource;
    List<byte[]> buffer;

    public int position = 0;
    public int samplerate = 44100;
    public float frequency = 440;

    AudioQueue queue;

    AudioRawDataManager AudioRawDataManager;

    // Use this for initialization
    void Start () {
        // get audio source
        audioSource = GameObject.Find("VideoCanvas").GetComponent<AudioSource>();
        //clip = AudioClip.Create("ClipName", samplerate * 2, 1, samplerate, false);
        //audioSource.clip = clip;

        queue = new AudioQueue();
        queue.audioSource = audioSource;

        CheckAppId();
        InitEngine();
        SetupAudio();
        JoinChannel();
	}

	// Update is called once per frame
	void Update () {
		PermissionHelper.RequestMicrophontPermission();
		PermissionHelper.RequestCameraPermission();
    }
	
	void CheckAppId()
    {
        logger = new Logger(logText);
        logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in VideoCanvas!!!!!");
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
        mRtcEngine.OnJoinChannelSuccess += OnJoinChannelSuccessHandler;
        mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        mRtcEngine.OnWarning += OnSDKWarningHandler;
        mRtcEngine.OnError += OnSDKErrorHandler;
        mRtcEngine.OnConnectionLost += OnConnectionLostHandler;
        mRtcEngine.OnUserJoined += OnUserJoinedHandler;
        mRtcEngine.OnUserOffline += OnUserOfflineHandler;

    }

    void SetupAudio() {
        // Initializes IRtcEngine.
        mRtcEngine = IRtcEngine.GetEngine(APP_ID);
        // Gets the AudioRawDataManager object.
        AudioRawDataManager = AudioRawDataManager.GetInstance(mRtcEngine);
        // Registers the audio observer.
        int result = AudioRawDataManager.RegisterAudioRawDataObserver();

        // mute agora audio playback
        mRtcEngine.SetParameter("che.audio.external_render", true);

        // Listens for the OnPlaybackAudioFrameHandler delegate.
        AudioRawDataManager.SetOnPlaybackAudioFrameCallback(OnPlaybackAudioFrameHandler);
    }


    // Gets a mixed audio frame of all remote users.
    void OnPlaybackAudioFrameHandler(agora_gaming_rtc.AudioFrame audioFrame)
    {
        // queue audio frame buffer data and play through unity audio source
        queue.Queue(audioFrame);
    }

    private float[] ConvertByteToFloat(byte[] array)
    {
        float[] floatArr = new float[array.Length / 4];
        for (int i = 0; i < floatArr.Length; i++)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(array, i * 4, 4);
            }
            floatArr[i] = BitConverter.ToSingle(array, i * 4) / 0x80000000;
        }
        return floatArr;
    }
  
    void JoinChannel()
    {
        mRtcEngine.JoinChannelByKey(TOKEN, CHANNEL_NAME, "", 0);
    }

	void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("onJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}", channelName, uid, elapsed));
        makeVideoView(0);
    }

    void OnLeaveChannelHandler(RtcStats stats)
    {
        logger.UpdateLog("OnLeaveChannelSuccess");
        DestroyVideoView(0);
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

    public enum AUDIO_FRAME_TYPE
    {
        // 0: PCM16
        FRAME_TYPE_PCM16 = 0,
    };

    public struct AudioFrame
    {
        // The type of the audio frame. See #AUDIO_FRAME_TYPE.
        public AUDIO_FRAME_TYPE type;
        // The number of samples per channel in the audio frame.
        public int samples;
        // The number of bytes per audio sample, which is usually 16-bit (2-byte).
        public int bytesPerSample;
        // The number of audio channels.
        // - 1: Mono
        // - 2: Stereo (the data is interleaved)
        public int channels;
        // The sample rate.
        public int samplesPerSec;
        // The data buffer of the audio frame. When the audio frame uses a stereo channel, the data buffer is interleaved. 
        // The size of the data buffer is as follows: buffer = samples × channels × bytesPerSample.
        public byte[] buffer;
        // The timestamp of the external audio frame. You can use this parameter for the following purposes:
        // - Restore the order of the captured audio frame.
        // - Synchronize audio and video frames in video-related scenarios, including where external video sources are used.
        public long renderTimeMs;
        // Reserved for future use.
        public int avsync_type;
    };



    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit");
        if (mRtcEngine != null)
        {
			mRtcEngine.LeaveChannel();
			mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }

    private void DestroyVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            UnityEngine.Object.Destroy(go);
        }
    }

    private void makeVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
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
        float yPos = UnityEngine.Random.Range(3.0f, 5.0f);
        float xPos = UnityEngine.Random.Range(-2.0f, 2.0f);
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
        //go.transform.Rotate(0f, 0.0f, 180f);
        float xPos = UnityEngine.Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        float yPos = UnityEngine.Random.Range(Offset, Screen.height / 2f - Offset);
        Debug.Log("position x " + xPos + " y: " + yPos);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(10f, 8f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }


}
