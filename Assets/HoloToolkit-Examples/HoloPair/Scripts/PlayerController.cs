// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

namespace HoloToolkit.Examples.HoloPair
{
    /// <summary>
    /// Controls player behavior (local and remote).
    /// </summary>
    [NetworkSettings(sendInterval = 0.033f)]
    public class PlayerController : NetworkBehaviour, IInputClickHandler
    {
        KeywordRecognizer keywordRecognizer = null;
        Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

        private NetworkManager NM;
        private PairingProcess PP;
        private PositionsManager positionManager;
        private InstructionsDisplay instructionsDisplay;
        private PairingCryptoManager CryptoManager;

        public GameObject arrowDirectionPointer;

        private BaseSecretChallenge secretChallenge;

        public GameObject secretPositionIndicator;
        public GameObject lineFromTo;

        /// <summary>
        /// Initial configuration for type of auth
        /// </summary>
        private bool initialShouldUseSecretColors = false;
        private int[] secretElementSizes = { 4, 6, 8 };

        /// <summary>
        /// The transform of the shared world anchor.
        /// </summary>
        private Transform sharedWorldAnchorTransform;

        #region Network communication client-server, server-client

        /// <summary>
        /// The position relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Vector3 localPosition;

        /// <summary>
        /// The rotation relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Quaternion localRotation;

        /// <summary>
        /// Sets the localPosition and localRotation on clients.
        /// </summary>
        /// <param name="postion">the localPosition to set</param>
        /// <param name="rotation">the localRotation to set</param>
        [Command]
        private void CmdTransform(Vector3 postion, Quaternion rotation)
        {
            if (!isLocalPlayer)
            {
                localPosition = postion;
                localRotation = rotation;
            }
        }

        [ClientRpc]
        private void RpcPlayerASendProtocolParameters(bool newShouldUseSecretColors, int numOfSecretElements, bool shouldAttackHappen)
        {
            PP.cnfgShouldUseSecretColors = newShouldUseSecretColors;
            PP.cnfgNumberOfSecretElements = numOfSecretElements;
            PP.isAttackHappening = shouldAttackHappen;
        }

        [ClientRpc]
        private void RpcPlayerASendPublicKey(string newPublicKeyA)
        {
            PP.msgPublicKeyA = newPublicKeyA;
        }

        [ClientRpc]
        private void RpcPlayerAAbort()
        {
            PP.msgProtocolAborted = true;
        }

        [Command]
        private void CmdMsgPlayerBAbort()
        {
            PP.msgProtocolAborted = true;
        }

        [Command]
        private void CmdMsgPlayerBRestarted()
        {
            PP.msgProtocolRestarted = true;
        }

        [Command]
        private void CmdPlayerBSendPublicKey(string newPublicKeyB)
        {
            PP.msgPublicKeyB = newPublicKeyB;
        }

        [ClientRpc]
        private void RpcPlayerASendFinalMessage(string newFinalMessage)
        {
            PP.msgFinalMessage = newFinalMessage;
        }

        [ClientRpc]
        private void RpcPlayerASendHashOfK(string newHashOfK)
        {
            PP.msgHashOfK = newHashOfK;
        }

        [ClientRpc]
        private void RpcPlayerASendEncryptedK(string newEncryptedK)
        {
            PP.msgEncryptedK = newEncryptedK;
        }

        [ClientRpc]
        private void RpcPlayerARestarted()
        {
            PP.msgProtocolRestarted = true;
        }

        [Command]
        private void CmdWriteLogLineToFile(string logLine)
        {
            PP.AppendStringToFile(logLine);
        }

        /// <summary>
        /// Called when the local player starts.  In general the side effect should not be noticed
        /// as the players' avatar is always rendered on top of their head.
        /// </summary>
        public override void OnStartLocalPlayer() { }

        #endregion

        #region User interaction methods

        // Callback method for all click from users
        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (isLocalPlayer)
            {
                RaycastHit hit;

                if (Physics.Raycast(transform.position, transform.forward, out hit))
                {
                    // If server player clicked on client player cube
                    if (hit.transform.gameObject.name == "Cube")   
                    {
                        userConfirmedStartPairing();
                        userConfirmedSecretChallengeOK();
                    }
                }
            }
        }

        // Initialize keywords and their callbacks 
        private void setupKeywordRecognizer()
        {
            // These are currently voice command but in future can switch to click
            // If attacker says abort or restart that is not a security issue, attacker can always do DoS
            keywords.Add("Abort", () => { UserSaidAbortOrCryptoFailed(); });
            keywords.Add("Restart", () => { UserSaidRestart(); });

            // Change Settings
            keywords.Add("Switch", () => { userSaidSwitch(); });
            keywords.Add("Update", () => { userSaidUpdate(); });
            keywords.Add("Change roles", () => { UserSaidChangeRoles(); });

            // Tell the KeywordRecognizer about our keywords.
            keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());

            // Register a callback for the KeywordRecognizer and start recognizing!
            keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
            keywordRecognizer.Start();
        }

        // When keyword is recognized, call callback method for that keyword 
        private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            System.Action keywordAction;
            if (keywords.TryGetValue(args.text, out keywordAction))
            {
                keywordAction.Invoke();
            }
        }

        private void userSaidUpdate()
        {
            // Update location of visual AR challenge
            if (isLocalPlayer)
            {
                secretChallenge.UpdateSharedHologramsLocation();
            }
        }

        private void userSaidSwitch()
        {
            // Switch between pipes and boxes types of auth
            if (isServer && isLocalPlayer)
            {
                PP.cnfgShouldUseSecretColors = !PP.cnfgShouldUseSecretColors;
                SetPairingSecretScheme(PP.cnfgShouldUseSecretColors);
                instructionsDisplay.setText("Confirmation Method Changed!");
                RestartProtocol();
            }
        }

        // Executed when user decides to abort, or protocol is aborted due to missmatch in cryptography.
        // We make sure that other side is notified and then clean up the state.
        private void UserSaidAbortOrCryptoFailed()
        {
            if (!isLocalPlayer) return;

            PP.SaveLogForPairingAttempt("ABORT");
            protocolAborted();

            // Notify the other player
            if (PP.isPlayerA)
            {
                RpcPlayerAAbort();
            }
            else
            {
                CmdMsgPlayerBAbort();
            }
        }

        private void UserSaidRestart()
        {
            // Restart pairing process
            if (isLocalPlayer)
            {
                PP.SaveLogForPairingAttempt("RESTART");
                RestartProtocol();
            }
        }

        private void UserSaidChangeRoles()
        {
            // For loggin purposes we need to know when users switch their devices so we can continue measurements
            if (isServer && isLocalPlayer)
            {
                instructionsDisplay.setText("Changing roles successful \n say \"RESTART\" when ready");
                PP.SwitchRoles();
            }
        }

        private void userConfirmedSecretChallengeOK()
        {
            // User confirmed that visual AR challenge was correct
            // if user A and in right step, change the variable
            if (isLocalPlayer && isServer && PP.currentStep == 7 && PP.allowedToClickPairingOK)
            {
                PP.userAConfirmedPairingSuccessful = true;
            }
        }

        private void userConfirmedStartPairing()
        {
            // if user A and in right step, then change the variable
            if (isLocalPlayer && isServer && PP.currentStep == 5)
            {
                PP.userAConfirmedVisualACK = true;
                PP.allowedToClickPairingOK = false;
                // After 2 seconds, allow that user clicks "pairing OK"
                StartCoroutine(Utils.ExecuteAfterDelay(2.0f, () => { PP.allowedToClickPairingOK = true; }));
            }
        }
        #endregion

        #region Methods controling user interface elements

        private void SetOtherPlayerColor(Color newColor)
        {
            GetComponentInChildren<MeshRenderer>().material.color = newColor;
        }

        private void HideARChallengeVisualElements()
        {
            secretChallenge.Remove();
        }

        
        // Tell position manager all location where am I and other players
        private void UpdateLocationsIfNeeded()
        {
            if (isServer)
            {
                if (isLocalPlayer)
                    positionManager.setServerLocation(Camera.main.transform.position);
                else
                    positionManager.setClientLocation(transform.localPosition);
            }
            else
            {
                if (isLocalPlayer)
                    positionManager.setClientLocation(Camera.main.transform.position);
                else
                    positionManager.setServerLocation(transform.position);
            }
        }

        #endregion

        #region Update loop for server

        private void runProtocolAsPlayerA()         // Player A is Server
        {
            if (PP.msgProtocolAborted) protocolAborted();
            if (PP.msgProtocolRestarted) initializeNewPairing();

            // Step 0: once player B has connected, send the shared protocol parameters.
            //    For now, we send:
            //         -- which type of confirmation step are we using: secretColors or secretNumbers
            //         -- how many secret elements should there be
            if (PP.currentStep == 0 && NM.numPlayers == 2)
            {
                // Choose the number of secret elements
                System.Random rand = new System.Random();
                PP.cnfgNumberOfSecretElements = secretElementSizes[rand.Next(0, secretElementSizes.Length)];
                RpcPlayerASendProtocolParameters(PP.cnfgShouldUseSecretColors, PP.cnfgNumberOfSecretElements, PP.shouldAttackHappen());
                PP.currentStep = 1;
            }

            // Step 1: User A sends his public key
            if (PP.currentStep == 1 && PP.msgPublicKeyA == "")
            {
                PP.AddTimeStampsToSteps(PP.currentStep);
                // generate userA's key pair
                CryptoManager.generateNewKeyPair();
                // send it to the server who will broadcast it to everyone else
                RpcPlayerASendPublicKey(CryptoManager.getPublicKey());
                PP.currentStep = 3; // go to the next step in the protocol
            }

            // Step 3: After receiving B's public key, user A makes a commitment on some value K
            if (PP.currentStep == 3 && PP.msgPublicKeyB != "")
            {
                PP.AddTimeStampsToSteps(PP.currentStep);
                // Player A generates those himself
                PP.myPrivateK = CryptoManager.generateRandomNonce();
                PP.myHashOfK = CryptoManager.generateHashFromString(PP.myPrivateK);
                RpcPlayerASendHashOfK(PP.myHashOfK);
                instructionsDisplay.setText("1. When other user waves, click on their cube.");

                PP.currentStep = 5;
            }

            // Step 5: After user A confirms that he has received an ACK from user B on the visual channel, we proceed
            if (PP.currentStep == 5 && PP.userAConfirmedVisualACK)
            {
                PP.AddTimeStampsToSteps(PP.currentStep);
                // Encrypt the plaintext K using B's public key
                string myEncryptedK = CryptoManager.encrypt(PP.myPrivateK, PP.msgPublicKeyB);

                RpcPlayerASendEncryptedK(myEncryptedK);
                PP.msgEncryptedK = myEncryptedK;

                // I can now also set all the colors 
                PP.myFinalSharedKey = PP.msgPublicKeyA + PP.msgPublicKeyB + PP.myPrivateK;
                setupSharedSecretVisualisation(PP.cnfgShouldUseSecretColors, PP.cnfgNumberOfSecretElements, PP.myFinalSharedKey);
                instructionsDisplay.setText("2. If gestures are correct, click on their cube.\nIf they make a mistake, say \"Abort\"");
                PP.currentStep = 7;
            }

            if (PP.currentStep == 7 && PP.userAConfirmedPairingSuccessful)
            {
                PP.AddTimeStampsToSteps(PP.currentStep);

                // Send a message in which a hash of our shared key is encrypted with B's private key
                string hashedSharedKey = CryptoManager.generateHashFromString(PP.myFinalSharedKey);
                RpcPlayerASendFinalMessage(hashedSharedKey);

                protocolSuccessful();
            }
        }

        #endregion

        #region Update loop for client

        private void runProtocolAsPlayerB()
        {
            if (PP.msgProtocolAborted) protocolAborted();
            if (PP.msgProtocolRestarted) initializeNewPairing();
            if (PP.currentStep == 0) { PP.currentStep = 2; }


            // Step 2: Generate and broadcast userB's public keypr
            if (PP.currentStep == 2 && PP.msgPublicKeyA != "")
            {
                // generate userB's public key
                CryptoManager.generateNewKeyPair();
                // send it to the server who will broadcast it to everyone else

                PP.msgPublicKeyB = CryptoManager.getPublicKey();
                CmdPlayerBSendPublicKey(PP.msgPublicKeyB);
                
                PP.currentStep = 4;
            }

            // Step 4: Acknowledge receipt of hashed value of K
            if (PP.currentStep == 4 && PP.msgHashOfK != "")
            {
                // player B stores the values that he receives
                PP.myHashOfK = PP.msgHashOfK;

                // We should tell the user to wave here! Or somehow differently acknowledge that he has received what he was supposed to.
                instructionsDisplay.setText("1. Wave to the other user!");
                PP.currentStep = 6;
            }

            // Step 6: Decrypt the plaintext value of K and confirm that it's OK.
            if (PP.currentStep == 6 && PP.msgEncryptedK != "")
            {
                PP.myPrivateK = CryptoManager.decryptWithMyKeypair(PP.msgEncryptedK);

                // Check if the value K matches the commitment.
                if (CryptoManager.generateHashFromString(PP.myPrivateK) != PP.myHashOfK)
                {
                    UserSaidAbortOrCryptoFailed(); // Crypto does not match!
                }

                if (PP.isAttackHappening)
                {
                    // If attack is happening, we simulate it by generating the sharedKeyK
                    //    in way as if received data was wrong.
                    PP.myFinalSharedKey = PP.msgPublicKeyB + PP.myPrivateK + PP.msgPublicKeyA;
                }
                else
                {
                    // If there is no attack, do it right.
                    PP.myFinalSharedKey = PP.msgPublicKeyA + PP.msgPublicKeyB + PP.myPrivateK;
                }
                
                setupSharedSecretVisualisation(PP.cnfgShouldUseSecretColors, PP.cnfgNumberOfSecretElements, PP.myFinalSharedKey);
                if (PP.cnfgShouldUseSecretColors)
                    instructionsDisplay.setText("2. Point to the cubes in the right order.");
                else
                    instructionsDisplay.setText("2. Follow the path with your finger.");

                PP.currentStep = 8;
            }

            if (PP.currentStep == 8 && PP.msgFinalMessage != "")
            {
                // Check if the hash of the sharedKey is the same as the received msgFinalMessage
                // If we are simulating an attack, this won't be detected since the attacker would supposedly be smart enough here?
                if (PP.isAttackHappening || CryptoManager.generateHashFromString(PP.myFinalSharedKey) == PP.msgFinalMessage)
                    protocolSuccessful();
                else
                    UserSaidAbortOrCryptoFailed(); // CryptoFailed
            }
        }

        #endregion

        #region Pairing procol methods

        private void initializeNewPairing()
        {
            PP.initializeNewPairing(isServer);
            HideARChallengeVisualElements();
            instructionsDisplay.setText("Waiting for others...");
        }

        private void SetPairingSecretScheme(bool showColors)
        {
            if (showColors)
            {
                secretChallenge = new ColorsSecretChallenge(isServer, 6);
            }
            else
            {
                secretChallenge = new PipesSecretChallenge(isServer, 6, secretPositionIndicator, lineFromTo);
            }
        }

        // Method to show secret AR conformation
        void setupSharedSecretVisualisation(bool shouldUseSecretColors, int numElements, string myFinalSecretKey)
        {
            secretChallenge.Show(numElements, myFinalSecretKey);
        }

        private void protocolSuccessful()
        {
            PP.SaveLogForPairingAttempt("SUCCESS");
            instructionsDisplay.setText("3. Pairing Successful!");
            PP.currentStep = -1;
            HideARChallengeVisualElements();
        }

        private void RestartProtocol()
        {
            if (PP.isPlayerA)
            {
                RpcPlayerARestarted();
            }
            else
            {
                CmdMsgPlayerBRestarted();
            }

            initializeNewPairing();
        }

        private void protocolAborted()
        {
            instructionsDisplay.setText("3. Pairing Failed!");
            PP.currentStep = -1;
            HideARChallengeVisualElements();
            RestartProtocol();
        }

        #endregion

        private void Start()
        {
            NM = GameObject.Find("UNETSharingStage").GetComponent<NetworkManager>();
            positionManager = GameObject.Find("PositionManager").GetComponent<PositionsManager>();
            positionManager.SetRole(isServer);
            PP = GameObject.Find("NetKeyboard").GetComponent<PairingProcess>();

            CryptoManager = GameObject.Find("CryptoManager").GetComponent<PairingCryptoManager>();
            instructionsDisplay = GameObject.Find("InstructionsHUD").GetComponent<InstructionsDisplay>();
            PP.cnfgShouldUseSecretColors = initialShouldUseSecretColors;
            SetPairingSecretScheme(PP.cnfgShouldUseSecretColors);

            if (isLocalPlayer)
            {
                setupKeywordRecognizer();
                initializeNewPairing();
            }

            if (SharedCollection.Instance == null)
            {
                Debug.LogError("This script required a SharedCollection script attached to a gameobject in the scene");
                Destroy(this);
                return;
            }

            if (isLocalPlayer)
            {
                // If we are the local player then we want to have airtaps 
                // sent to this object so that projeciles can be spawned.
                InputManager.Instance.AddGlobalListener(gameObject);
            }
            else
            {
                Debug.Log("remote player");
                Color baseColor = Color.yellow;
                Color otherPlayerColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f);
                SetOtherPlayerColor(otherPlayerColor);
            }

            sharedWorldAnchorTransform = SharedCollection.Instance.gameObject.transform;
            transform.SetParent(sharedWorldAnchorTransform);
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                if (isServer)
                {
                    runProtocolAsPlayerA();
                }
                else if (!isServer)
                {
                    runProtocolAsPlayerB();
                }


                // ------------- Handle positioning and different player's locations

                // if we are the remote player then we need to update our worldPosition and then set our 
                // local (to the shared world anchor) position for other clients to update our position in their world.
                transform.position = Camera.main.transform.position;
                transform.rotation = Camera.main.transform.rotation;

                // Depending on if you are host or client, either setting the SyncVar (client) 
                // or calling the Cmd (host) will update the other users in the session.
                // So we have to do both.
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;

                CmdTransform(localPosition, localRotation);
            }
            else
            {

                // If we aren't the local player, we only need to make sure that the position of this object is set properly
                // so that we properly render their avatar in our world.           

                transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, 0.3f);
                transform.localRotation = localRotation;
            }

            UpdateLocationsIfNeeded();
        }

        private void OnDestroy()
        {
            if (isLocalPlayer)
            {
                InputManager.Instance.RemoveGlobalListener(gameObject);
            }
        }
    }
}
