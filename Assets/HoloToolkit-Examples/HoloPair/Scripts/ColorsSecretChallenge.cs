using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Examples.HoloPair
{
    public class ColorsSecretChallenge : BaseSecretChallenge
    {
        public ColorsSecretChallenge(bool isServer, int numberOfSecretElements) : base()
        {
            base.isServer = isServer;
            base.numberOfSecretElements = numberOfSecretElements;
        }

        public override void Show(int numOfSecretPositions, string myFinalSharedSecret)
        {
            SetupSecretColors(numOfSecretPositions, myFinalSharedSecret);
            base.UpdateSharedHologramsLocation();
        }

        public override void Remove()
        {
            setAllBoxesVisibility(false);
        }

        protected void SetupSecretColors(int numOfSecretElements, string myFinalSharedSecret)
        {
            // secretColoring will be twice the size: element 2*i is the color, element 2*i+1 is orientation
            int[] secretColoring = generateSecretColoringFromString(myFinalSharedSecret, numOfSecretElements);

            Color[] availableColors = new Color[4] {
                Color.red,
                Color.green,
                Color.blue,
                Color.white
            };

            // First hide all of them, but then show those which we don't need.
            setAllBoxesVisibility(false);
            for (int i = 0; i < numOfSecretElements; i++)
            {
                string currentBoxName = "SecretColor" + i;
                setElementVisibility(currentBoxName, true);

                int secretColorIndex = isServer ? i : numOfSecretElements - i - 1;
                setObjectColor(currentBoxName, availableColors[secretColoring[secretColorIndex]]);
            }

            for (int i = 1; i <= 4; i++)
            {
                setElementVisibility("ButtonCube" + i.ToString(), true);
                setObjectColor("ButtonCube" + i.ToString(), availableColors[i - 1]);
            }
        }

        protected int[] generateSecretColoringFromString(string myFinalSharedSecret, int numOfSecretElements) {
            return CryptoManager.generateSecretColoringFromString(myFinalSharedSecret, numOfSecretElements);
        }

        private void setAllBoxesVisibility(bool visibility)
        {
            for (int i = 0; i < maxNumSecretElements; i++)
            {
                setElementVisibility("SecretColor" + i.ToString(), visibility);
                setElementVisibility("secretColorIndex" + i.ToString(), visibility);
            }

            for (int i = 1; i <= 4; i++)
            {
                setElementVisibility("ButtonCube" + i.ToString(), visibility);
            }
        }

        private void setElementVisibility(string element, bool visibility)
        {
            GameObject box = GameObject.Find(element);
            if (box != null)
            {
                box.GetComponent<Renderer>().enabled = visibility;
            }
        }

        void setObjectColor(string boxName, Color color)
        {
            GameObject box = GameObject.Find(boxName);
            MeshRenderer mr = box.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Diffuse"));
            mr.material.color = color;
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

