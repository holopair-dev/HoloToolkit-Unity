using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Examples.HoloPair
{
    public class PositionsManager : MonoBehaviour
    {
        /// <summary>
        /// One central repository for locations of players in pairing process
        /// </summary>
        private Vector3 serverLocation;
        private Vector3 clientLocation;
        private bool isServer;

        public void setServerLocation(Vector3 point)
        {
            this.serverLocation = point;
        }

        public Vector3 getServerLocation()
        {
            return this.serverLocation;
        }

        public void setClientLocation(Vector3 point)
        {
            this.clientLocation = point;
        }

        public Vector3 getClientLocation()
        {
            return this.clientLocation;
        }

        public Vector3 getSelfPosition()
        {
            return isServer ? this.serverLocation : this.clientLocation;
        }
        public Vector3 getOthersPosition()
        {
            return isServer ? this.clientLocation : this.serverLocation;
        }

        public void SetRole(bool isServer)
        {
            // Position manager should know while role is he do other can ask him to give them their position
            this.isServer = isServer;
        }

        public void MoveObjectToPositionThroughTime(Transform movingObject, Vector3 endPos, float timeForTranslation)
        {
            StartCoroutine(Utils.TranslateTo(movingObject, endPos, timeForTranslation));
        }

        void Start()
        {
            // Default position until shared anchor sets up its position 
            this.serverLocation = new Vector3(0f, 0f, 0f);
            this.clientLocation = new Vector3(0f, 0f, 0f);
        }

        void Update()
        {

        }
    }
}

