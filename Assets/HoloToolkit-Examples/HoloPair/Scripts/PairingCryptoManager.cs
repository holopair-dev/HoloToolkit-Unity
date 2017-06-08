using System;
using System.Text;
using UnityEngine;
#if NETFX_CORE
    using Windows.Security.Cryptography.Core;
    using Windows.Security.Cryptography;
    using Windows.Storage.Streams;
#endif

namespace HoloToolkit.Examples.HoloPair
{
    public class PairingCryptoManager : MonoBehaviour
    {

#if NETFX_CORE
        // Open the algorithm provider for the specified asymmetric algorithm.
        private AsymmetricKeyAlgorithmProvider objAlgProv;
        //    private KeyDerivationAlgorithmProvider objKdfProv; // uncomment if we start using symmetric key derivation from a shared secret
        private CryptographicKey myKeyPair;
#endif
        private string myPublicKeyString = "";

        // --------- HASHING -------------------

        public string generateRandomNonce()
        {
#if NETFX_CORE
            IBuffer buffNonce = CryptographicBuffer.GenerateRandom(32); 
            Debug.Log("NONCE: " + cryptoBufferToString(buffNonce));
            return cryptoBufferToString(buffNonce);
#else
        System.Random rand = new System.Random();
        string newNonce = "nonce_" + rand.Next().ToString();
        return newNonce;
#endif
        }

        public string generateHashFromString(string strMsg)
        {
#if NETFX_CORE
            string strAlgName = HashAlgorithmNames.Sha256;

            // Convert the message string to binary data.
            IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(strMsg, BinaryStringEncoding.Utf8);

            // Create a HashAlgorithmProvider object.
            HashAlgorithmProvider hashAlgProv = HashAlgorithmProvider.OpenAlgorithm(strAlgName);

            // Hash the message.
            IBuffer buffHash = hashAlgProv.HashData(buffUtf8Msg);

            // Verify that the hash length equals the length specified for the algorithm.
            if (buffHash.Length != hashAlgProv.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }

            // Convert the hash to a string (for display).
            string strHash = cryptoBufferToString(buffHash);

            // Return the encoded string
            return strHash;

#else
        return "";
#endif

        }

        public float[] generateSecretPositionsFromString(string stringToHashFrom, int numPositions)
        {
            int maxElement = 10;
            int scale = 100;
            float multiplier = 2.5f;
            // max element that we return is actually maxElement / scale * multiplier: 0.3 in our case!
            // entropy is maxElement^(2N)

            float[] secretPositions = new float[2 * numPositions];
            string myHash = generateHashFromString(stringToHashFrom);
            int[] extractedInts = getArrayOfIntsFromHash(myHash, 2 * numPositions, 2 * maxElement + 1);  // for x and y coordinates, between -10 and 10

            for (int i = 0; i < numPositions; ++i)
            {
                secretPositions[2 * i] = (float)(-maxElement + extractedInts[2 * i]) / (float)scale * multiplier;
                secretPositions[2 * i + 1] = (float)(-maxElement + extractedInts[2 * i + 1]) / (float)scale * multiplier;
            }
            return secretPositions;
        }

        public int[] generateSecretColoringFromString(string stringToHashFrom, int numberOfSecretElements)
        {
            int maxElement = 4;
            string myHash = generateHashFromString(stringToHashFrom);
            // We need twice as many elements: one for color, one for rotation
            return getArrayOfIntsFromHash(myHash, numberOfSecretElements * 2, maxElement);
        }

        private int[] getArrayOfIntsFromHash(string myHash, int numberOfRequiredElements, int maxElement)
        {
            Debug.Log("generatedStrongHash: " + myHash);
            // convert stringHash to asciiValues
            byte[] hashBytes = Convert.FromBase64String(myHash);
            int[] result = new int[numberOfRequiredElements];

            long longRunningVal = 0;
            int currentByte = 0;
            for (int i = 0; i < numberOfRequiredElements; ++i)
            {
                if (longRunningVal * (long)256 + hashBytes[currentByte] < (long)Int32.MaxValue)
                {
                    longRunningVal = longRunningVal * 256 + hashBytes[currentByte];
                    ++currentByte;
                }

                result[i] = (int)longRunningVal % maxElement;
                longRunningVal /= maxElement;
            }

            return result;
        }


        // ------------ KEYPAIRS, ENCRYPTION and DECRYPTION ---------------

        public void generateNewKeyPair()
        {
#if NETFX_CORE
            // Create an asymmetric key pair.
            UInt32 keyLength = 2048; // This is a must because of the algorithm that we currently use
            myKeyPair = objAlgProv.CreateKeyPair(keyLength);
            // Export the public key to a buffer for use by others.
            IBuffer buffPublicKey = myKeyPair.ExportPublicKey();

            myPublicKeyString = cryptoBufferToString(buffPublicKey);
            Debug.Log("NOVI KLJUC: |" + myPublicKeyString + "|");
#endif
        }

        public string getPublicKey()
        {
            return myPublicKeyString;
        }

#if NETFX_CORE
        // used to convert crypto keys from strings to crypto IBuffers
        private IBuffer cryptoStringToBuffer(string str)
        {
            return CryptographicBuffer.DecodeFromBase64String(str);
        }

        // Used when we want to convert generic strings, which are not crypto keys
        private IBuffer genericStringToBuffer(string str)
        {
            byte[] myBytes = Encoding.UTF8.GetBytes(str);
            return CryptographicBuffer.CreateFromByteArray(myBytes);
        }

        private string cryptoBufferToString(IBuffer buff)
        {
            return CryptographicBuffer.EncodeToBase64String(buff);
        }

        private string genericBufferToString(IBuffer buff)
        {
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buff); ;
        }
#endif

        public string encrypt(string plaintext, string publicKeyString)
        {
#if NETFX_CORE
            // Import the public key from a buffer.
            CryptographicKey publicKey = objAlgProv.ImportPublicKey(cryptoStringToBuffer(publicKeyString));

            // Encrypt some data using the public key (not the keypair!).
            IBuffer buffEncryptedData = CryptographicEngine.Encrypt(publicKey, genericStringToBuffer(plaintext), null);

            string encryptedData = cryptoBufferToString(buffEncryptedData);
            return encryptedData;
#else
        return "";
#endif
        }

        public string decryptWithMyKeypair(string encryptedText)
        {
#if NETFX_CORE
            IBuffer buffDecryptedData = CryptographicEngine.Decrypt(myKeyPair, cryptoStringToBuffer(encryptedText), null);
            string decryptedData = genericBufferToString(buffDecryptedData);

            return decryptedData;
#else
        return "";
#endif
        }

        // Use this for initialization
        void Start()
        {
#if NETFX_CORE
            objAlgProv = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
#endif
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

