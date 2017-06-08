using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Examples.HoloPair
{
    public abstract class BaseSecretChallenge : MonoBehaviour
    {
        protected PairingCryptoManager CryptoManager;
        protected PositionsManager positionManager;

        protected bool isServer;
        protected int numberOfSecretElements = 0;
        protected int maxNumSecretElements = 8;

        protected BaseSecretChallenge()
        {
            this.CryptoManager = GameObject.Find("CryptoManager").GetComponent<PairingCryptoManager>();
            this.positionManager = GameObject.Find("PositionManager").GetComponent<PositionsManager>();
        }

        public virtual void Show(int numOfSecretPositions, string myFinalSharedSecret) { }

        public virtual void Remove() { }


        // TODO: we could have one update which is immediate and one which is with movement
        public void UpdateSharedHologramsLocation()
        {
            // so that the location is properly set between two players.
            float startDistFromPlayerB = 1.5f;
            float endDistFromPlayerB = 0.85f; // in meters
            float timeForTranslation = 3.0f; // in seconds

            Vector3 dir = (positionManager.getServerLocation() - positionManager.getClientLocation()).normalized;

            // TODO change this is on keyboard 
            GameObject netKeyboard = GameObject.Find("NetKeyboard");
            netKeyboard.transform.position = positionManager.getClientLocation() + dir * startDistFromPlayerB;
            Vector3 endPos = positionManager.getClientLocation() + dir * endDistFromPlayerB;
            positionManager.MoveObjectToPositionThroughTime(netKeyboard.transform, endPos, timeForTranslation);

            // Rotate the position of the shared elements if I am the player A
            if (isServer)
            {
                netKeyboard.transform.rotation = Quaternion.LookRotation(netKeyboard.transform.position - Camera.main.transform.position);
            }
            else
            {
                netKeyboard.transform.LookAt(Camera.main.transform);
            }
        }
    }
 }

