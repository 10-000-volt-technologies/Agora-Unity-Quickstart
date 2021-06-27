using UnityEngine;
using UnityEngine.UI;

namespace NNCam {

public sealed class Compositor : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] WebcamInput _input = null;
    [SerializeField] Texture2D _background = null;
    [SerializeField, Range(0.01f, 0.99f)] float _threshold = .5f;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] Shader _shader = null;

    #endregion

    #region Internal objects

    SegmentationFilter _filter;
    Material _material;
        RenderTexture _miniWebcamRT;
        #endregion

        #region MonoBehaviour implementation

        void Start()
    {
            _miniWebcamRT = new RenderTexture(1920, 1080, 0);

            _filter = new SegmentationFilter(_resources);
        _material = new Material(_shader);

        }

        void OnDestroy()
    {
        _filter.Dispose();
        Destroy(_material);
    }

        void Update()
        {
            _filter.ProcessImage(_input.Texture);
            if (GameObject.Find("MiniWebcam").GetComponent<RawImage>().texture == null)
                GameObject.Find("MiniWebcam").GetComponent<RawImage>().texture = _miniWebcamRT;

            _material.SetTexture("_Background", _background);
            _material.SetTexture("_CameraFeed", _input.Texture);
            _material.SetTexture("_Mask", _filter.MaskTexture);
            _material.SetFloat("_Threshold", _threshold);
            Graphics.Blit(null, _miniWebcamRT, _material, 0);
        }



    #endregion
}

} // namespace NNCam
