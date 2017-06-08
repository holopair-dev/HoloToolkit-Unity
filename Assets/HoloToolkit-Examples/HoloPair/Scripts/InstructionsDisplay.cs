using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoloToolkit.Examples.HoloPair
{
    public class InstructionsDisplay : MonoBehaviour
    {
        private PositionsManager positionManager;

        /// <summary>
        /// Sets text on instructions HUD display
        /// </summary>
        /// <param name="text"></param>
        public void setText(string text)
        {
            var hud = GameObject.Find("StepsText").GetComponent<Text>();
            hud.text = text;
        }

        // Use this for initialization
        void Start()
        {
            // Position manager is used to give us shared coordinates of players in pairing process
            positionManager = GameObject.Find("PositionManager").GetComponent<PositionsManager>();
        }

        // Update is called once per frame
        void Update()
        {
            // Choose where to place it
            Vector3 otherPos = positionManager.getOthersPosition();
            Vector3 myPos = positionManager.getSelfPosition();

            // Set position slightly above the other player's head
            this.transform.position = new Vector3(otherPos.x, otherPos.y + 0.3f, otherPos.z);
            // Make the text face me 
            this.transform.rotation = Quaternion.LookRotation(otherPos - myPos);
        }
    }

}
