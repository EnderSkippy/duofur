using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Button3D : MonoBehaviour
{
    public enum uisound
    {
        tap,
        bigTap,
        ting,
        help,
        buy,
        none,
        systemOpen,
        systemClose,
        deny,
        create,
        unboxCrate,
        sitDown
    }

    public GameObject ui;
    public string funcName;
    public int funcWindow;
    public bool sendPlayerNum;
    public float clickTime;
    public string buttonText;
    public uisound uiSound = uisound.tap;
    public bool ignoreCollider;
    private bool click;
    private AudioSource sc;

    private void Start()
    {
        if (!ignoreCollider) StartCoroutine(WaitForRectTransform());

        sc = GameObject.Find("GlobalAudio").GetComponent<AudioSource>();
    }


    // Update is called once per frame
    private void Update()
    {
        if (click) clickTime += Time.deltaTime;
        if (clickTime > 0.2f)
        {
            clickTime = 0;
            click = false;
        }
    }

    private void OnDisable()
    {
        click = false;
    }


    private IEnumerator WaitForRectTransform()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        int attempts = 3;

        while (attempts > 0 && rectTransform.rect.width == 0)
        {
            attempts--;
            yield return null; // wait for the next frame
        }

        if (rectTransform.rect.width == 0)
        {
            Debug.LogError(
                $"RectTransform width is still 0 after multiple attempts on button {gameObject.name}. Hiding button...");
            gameObject.SetActive(false);
        }
        else
        {
            GetComponent<BoxCollider>().size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0f);
        }
    }

    public void StartClick(string name)
    {
        click = true;
    }

    public void EndClick(string name)
    {
        if (click)
            if (funcName != "")
            {
                if (sc == null) sc = GameObject.Find("GlobalAudio").GetComponent<AudioSource>();
                switch (uiSound)
                {
                    case uisound.tap:

                        sc.clip = (AudioClip)Resources.Load("tap");
                        sc.pitch = Random.Range(0.95f, 1.05f);
                        break;
                    case uisound.bigTap:
                        sc.clip = (AudioClip)Resources.Load("big tap");
                        sc.pitch = Random.Range(0.98f, 1.02f);
                        break;
                    case uisound.ting:
                        sc.clip = (AudioClip)Resources.Load("ting");
                        sc.pitch = Random.Range(0.95f, 1.05f);
                        break;
                    case uisound.help:
                        sc.clip = (AudioClip)Resources.Load("help");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.buy:
                        sc.clip = (AudioClip)Resources.Load("Purchase");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.none:
                        sc.clip = null;
                        break;
                    case uisound.systemOpen:
                        sc.clip = (AudioClip)Resources.Load("SystemOpen");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.systemClose:
                        sc.clip = (AudioClip)Resources.Load("SystemClose");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.deny:
                        sc.clip = (AudioClip)Resources.Load("Deny");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.create:
                        sc.clip = (AudioClip)Resources.Load("Create");
                        sc.pitch = 1.0f;
                        break;
                    case uisound.unboxCrate:
                        sc.clip = (AudioClip)Resources.Load("Crate Unbox");
                        sc.pitch = 1.0f;
                        break;
                }

                sc.Play();
                click = false;

                int finalsend = funcWindow;

                if (sendPlayerNum)
                {
                    if (name == "Player")
                        finalsend = 0;
                    else
                        finalsend = 1;
                }

                ui.SendMessage(funcName, finalsend);
            }
    }
}