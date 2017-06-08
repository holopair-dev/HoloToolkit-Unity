using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Examples.HoloPair
{
    public class PipesSecretChallenge : BaseSecretChallenge
    {

        /// <summary>
        /// GameObjects needed to draw pipes
        /// </summary>
        public GameObject secretPositionIndicator;
        public GameObject lineFromTo;

        public PipesSecretChallenge(bool isServer, int numberOfSecretElements, GameObject secretPositionIndicator, GameObject lineFromTo) : base() 
        {
            base.isServer = isServer;
            base.numberOfSecretElements = numberOfSecretElements;
            this.secretPositionIndicator = secretPositionIndicator;
            this.lineFromTo = lineFromTo;
        }

        public override void Show(int numOfSecretPositions, string myFinalSharedSecret)
        {
            SetupSecretPositions(numOfSecretPositions, myFinalSharedSecret);
            base.UpdateSharedHologramsLocation();
        }

        public override void Remove()
        {
            RemoveAllPositionIndicators();
        }

        private void RemoveAllPositionIndicators()
        {
            foreach (var currentObject in GameObject.FindGameObjectsWithTag("secret-position-indicator"))
            {
                Destroy(currentObject);
            }
        }

        private void SetLineDimensionsFromTo(GameObject objectToTransform, Transform posStart, Transform posEnd)
        {
            objectToTransform.transform.position = (posEnd.position + posStart.position) / 2.0f;  // it should be positioned at half
            objectToTransform.transform.localScale = new Vector3(objectToTransform.transform.localScale.x, 0.95f * (posEnd.localPosition - posStart.localPosition).magnitude / 2.0f, objectToTransform.transform.localScale.z); // set the correct local scale
            objectToTransform.transform.localRotation = Quaternion.FromToRotation(Vector3.up, posEnd.position - posStart.position);  // rotate properly
        }

        private void SetupSecretPositions(int numOfSecretPositions, string myFinalSharedSecret)
        {
            float[] secretPositions = CryptoManager.generateSecretPositionsFromString(myFinalSharedSecret, numOfSecretPositions);

            Transform previousSecretTransform = null;
            for (int i = 0; i < numOfSecretPositions; ++i)
            {
                // Generate numbers
                GameObject newSecretPositionIndicator = (GameObject)Instantiate(secretPositionIndicator);
                Transform indicatorTransform = newSecretPositionIndicator.transform;
                indicatorTransform.parent = GameObject.Find("NetKeyboard").transform;
                indicatorTransform.localRotation = Quaternion.identity; // make the object aligned with the parent.
                newSecretPositionIndicator.GetComponent<TextMesh>().text = (i + 1).ToString();
                if (i == 0) { newSecretPositionIndicator.GetComponent<TextMesh>().fontSize *= 2; } // make number 1 more obvious
                indicatorTransform.localPosition = new Vector3(secretPositions[2 * i], secretPositions[2 * i + 1], 0f); // change 0f to something negative if we want to move it a bit farther from userB

                if (!isServer)
                {
                    // Rotate again around y-axis if I am player B (because the whole NetKeyboard will be rotated once again
                    indicatorTransform.RotateAround(indicatorTransform.position, indicatorTransform.up, 180f);
                }

                // Generate lines between the previous and the current secret position?
                if (i > 0)
                {
                    GameObject newSecretLine = (GameObject)Instantiate(lineFromTo);
                    // , new Vector3(0f, 0f, 0f), Quaternion.Euler(new Vector3(0f, 0f, 0f)));
                    SetLineDimensionsFromTo(newSecretLine, previousSecretTransform, newSecretPositionIndicator.transform);
                    newSecretLine.transform.parent = GameObject.Find("NetKeyboard").transform;
                }

                previousSecretTransform = newSecretPositionIndicator.transform;
            }
        }

    }

}
