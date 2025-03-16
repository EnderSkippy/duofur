using UnityEngine;

namespace Show
{
    public class Video : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MeshRenderer renderer;
        private ShowController showController;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
            renderer = gameObject.GetComponent<MeshRenderer>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (showController.active)
                renderer.material.SetColor(BaseColor, Color.black);
            else
                renderer.material.SetColor(BaseColor, Color.white);
        }
    }
}