using System.Collections.Generic;
using UnityEngine;

namespace Show
{
    public class Curtains : MonoBehaviour
    {
        [Range(0.0001f, .01f)] public float[] flowControlOut;

        [Range(0.0001f, .01f)] public float[] flowControlIn;

        public bool[] curtainbools;
        public bool curtainOverride;
        private readonly List<int> cylindersBottom = new();
        private readonly List<int> cylindersTop = new();

        private ShowController _showController;

        private Animator characterValves;

        private void Start()
        {
            characterValves = GetComponent<Animator>();
            for (int e = 0; e < characterValves.layerCount; e++)
            {
                string temp = characterValves.GetLayerName(e);
                if (temp[temp.Length - 1] == 'T')
                    cylindersTop.Add(int.Parse(temp.Substring(0, temp.Length - 1)));
                else
                    cylindersBottom.Add(int.Parse(temp.Substring(0, temp.Length - 1)));
            }

            curtainbools = new bool[characterValves.layerCount];
            _showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
        }

        private void Update()
        {
            if (_showController.active) CreateMovements(Time.deltaTime * _showController.updateRate);
        }

        public void CreateMovements(float num3)
        {
            //Loop through cylinders to update
            for (int i = 0; i < cylindersTop.Count; i++)
                //Get current animation value
                SetTime("T", cylindersTop[i], _showController.topDrawer, i, num3);
            for (int i = 0; i < cylindersBottom.Count; i++)
                //Get current animation value
                SetTime("B", cylindersBottom[i], _showController.bottomDrawer, i, num3);
        }

        private void SetTime(string drawername, int currentAnim, bool[] drawer, int lasti, float num3)
        {
            //Cycle through parameters to find matching code
            for (int e = 0; e < characterValves.parameters.Length; e++)
                if (characterValves.parameters[e].name.Substring(0, characterValves.parameters[e].name.Length - 1) ==
                    currentAnim.ToString())
                {
                    //Calculate next value of the parameter
                    float nextTime = characterValves.GetFloat(currentAnim + drawername);

                    //Check if animation is already done
                    if (!curtainOverride)
                    {
                        bool check = drawer[currentAnim - 1];

                        if (nextTime == 0 && !check) break;
                        if (nextTime == 1 && check) break;
                    }

                    //Set bool
                    if (drawer[currentAnim - 1])
                        curtainbools[lasti] = true;
                    else if (drawer[currentAnim + 1 - 1]) curtainbools[lasti] = false;

                    //Set Curtain
                    if (!curtainOverride)
                    {
                        if (curtainbools[lasti])
                            nextTime += flowControlOut[e] * 1.25f * num3;
                        else
                            nextTime -= flowControlIn[e] * 1.25f * num3;
                    }
                    else
                    {
                        nextTime += flowControlOut[e] * 1.25f * num3;
                    }

                    nextTime = Mathf.Min(Mathf.Max(nextTime, 0), 1);

                    //Apply parameter
                    characterValves.SetFloat(currentAnim + drawername, nextTime);
                    break;
                }
        }
    }
}