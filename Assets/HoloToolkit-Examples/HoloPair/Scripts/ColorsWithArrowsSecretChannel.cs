using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HoloToolkit.Examples.HoloPair
{
    public class ColorsWithArrowsSecretChannel : ColorsSecretChallenge
    {
        GameObject arrowDirectionPointer; 

        public ColorsWithArrowsSecretChannel(bool isServer, int numberOfSecretElements, GameObject arrowDirectionPointer) : base(isServer, numberOfSecretElements)
        {
            this.arrowDirectionPointer = arrowDirectionPointer;
        }

        public override void Show(int numOfSecretColors, string myFinalSharedSecret)
        {
            SetupSecretColors(numOfSecretColors, myFinalSharedSecret);
            int[] secretColoring = generateSecretColoringFromString(myFinalSharedSecret, numOfSecretColors);

            for (int i = 0; i < numOfSecretColors; i++)
            {
                int secretColorIndex = isServer ? i : numOfSecretColors - i - 1;
                AddArrow(secretColoring[secretColorIndex + numOfSecretColors], i);
            }

            base.UpdateSharedHologramsLocation();
        }

        private void AddArrow(int rotationInNumber, int arrowIdx)
        {
            string[] directions = new string[4]
            {
                "↑", "→", "↓", "←"
            };

            GameObject secretColorObject = GameObject.Find("SecretColor" + arrowIdx);
            if (arrowDirectionPointer == null)
            {
                return;
            }
            GameObject newArrowObject = (GameObject)Instantiate(arrowDirectionPointer);
            newArrowObject.transform.parent = secretColorObject.transform;
            newArrowObject.transform.localRotation = Quaternion.identity; // make the object aligned with the parent.
            newArrowObject.transform.localPosition = new Vector3(0f, 0f, 0f); // change 0f to something negative if we want to move it a bit farther from userB

            TextMesh text = newArrowObject.GetComponent<TextMesh>();
            // On client we need to switch right and left
            if (isServer)
            {
                if (rotationInNumber == 1)
                {
                    rotationInNumber = 3;
                }
                else if (rotationInNumber == 3)
                {
                    rotationInNumber = 1;
                }
            }
            text.text = directions[rotationInNumber];
        }
    }

}
