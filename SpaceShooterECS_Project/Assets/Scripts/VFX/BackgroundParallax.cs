using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BackgroundParallax : MonoBehaviour {

    public Material MaterialToUse;
    private Material MaterialInstance;


    public float LayerTransparency = 1.0f;
    public float ScrollSpeed = 1.0f;

    public Vector2 LayerScrollDirection = new Vector2(-1.0f, 0.0f);

    private MeshRenderer[] _backgroundRenderers = null;

    private void Awake()
    {
        _backgroundRenderers = GetComponentsInChildren<MeshRenderer>();
        Assert.AreNotEqual(_backgroundRenderers.Length, 0, "Object: " + gameObject.name +
            " requires mesh renderers for parallax scrolling mechanism, add components to object.");

        MaterialInstance = new Material(MaterialToUse);
        MaterialInstance.name = MaterialInstance.name + "_Instance";

        for(int i = 0; i < _backgroundRenderers.Length; i++)
        {
            _backgroundRenderers[i].material = MaterialInstance;
        }
    }

    // Use this for initialization
    void Start () {
    }

	// Update is called once per frame
	void Update ()
    {
        Vector2 newTexOffset = MaterialInstance.mainTextureOffset;
        newTexOffset.x += (ScrollSpeed * Time.deltaTime * LayerScrollDirection.x);
        newTexOffset.y += (ScrollSpeed * Time.deltaTime * LayerScrollDirection.y);
        MaterialInstance.mainTextureOffset = newTexOffset;
    }
}
