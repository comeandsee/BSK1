﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BSKprojekt1
{
    public static class EncryptionHelper
    {

        public static CipherMode CipherModeFromString(string stringMode)
        {
            CipherMode mode = CipherMode.CBC;
            switch (stringMode)
            {
                case Globals.modeCBC:
                    mode = CipherMode.CBC;
                    break;
                case Globals.modeCFB:
                    mode = CipherMode.CFB;
                    break;
                case Globals.modeECB:
                    mode = CipherMode.ECB;
                    break;
                case Globals.modeOFB:
                    mode = CipherMode.OFB;
                    break;
            }

            return mode;
        }


    //based on https://msdn.microsoft.com/pl-pl/library/system.security.cryptography.aes(v=vs.110).aspx
    //encrypts file from path srcFileName to file in path destFileName
    public static void AesEncryptFromFile(string srcFileName, string destFileName, 
            byte[] key, CipherMode mode, int blockSize, out byte[] IV, BackgroundWorker worker)
        {
            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();
                aesAlg.Mode = mode;
                aesAlg.BlockSize = blockSize;
                //aesAlg.Padding = PaddingMode.Zeros;

                IV = (aesAlg.IV).ToArray();
                
                
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();

                //based on: https://stackoverflow.com/questions/9237324/encrypting-decrypting-large-files-net
                using (FileStream destFileStream = new FileStream(destFileName, FileMode.Create,
                            FileAccess.Write, FileShare.None))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(destFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (FileStream source = new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            Stopwatch stopwatch = Stopwatch.StartNew();

                            // source.CopyTo(cryptoStream);

                            int bitsInBuffer = 64 * 1024;
                            byte[] buffer = new byte[bitsInBuffer];//todo decide on size
                            int data, count = 1;
                            double progress;
                            while((data = source.Read(buffer, 0, buffer.Length)) > 0)
                            {

                                cryptoStream.Write(buffer, 0, data);
                                progress = ((double)count* bitsInBuffer / source.Length)*100;
                                worker.ReportProgress((int)progress);
                                count++;
                            }
                            stopwatch.Stop();
                            Console.WriteLine(stopwatch.ElapsedMilliseconds);
                            
                        }
                    }
                }

                /*using (FileStream destFileStream = new FileStream(destFileName, FileMode.Create, 
                        FileAccess.Write, FileShare.None))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(destFileStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (FileStream source = new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                source.CopyTo(cryptoStream);
                            }
                        }
                    }*/


                            /*
                            using (MemoryStream msEncrypt = new MemoryStream())
                            {
                                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                                {
                                    //when encoding strings
                                     (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                                    {
                                        swEncrypt.Write(fileText);
                                    }
                                    encrypted = msEncrypt.ToArray();


                                }
                            }*/
            }
         }

        //headerByteLength- offset after which the encoded file in encodedFileName starts
        public static void AesDecryptToFile(string encodedFileName, 
            string decodedFileName, byte[] sessionKey, CipherMode mode, 
            int blockSize, byte[] IV)
        {
            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = sessionKey;
                aesAlg.Mode = mode;
                aesAlg.BlockSize = blockSize;
                aesAlg.IV = IV;
                //aesAlg.Padding = PaddingMode.Zeros;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor();

                //based on: https://stackoverflow.com/questions/9237324/encrypting-decrypting-large-files-net
                using (FileStream destination = new FileStream(decodedFileName, FileMode.CreateNew, 
                    FileAccess.Write, FileShare.None))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(destination, decryptor, CryptoStreamMode.Write))
                    {
                        try
                        {
                            using (FileStream source = new FileStream(encodedFileName, FileMode.Open, 
                                FileAccess.Read, FileShare.Read))
                            {
                                //source.CopyTo(destination);
                                
                                source.CopyTo(cryptoStream);
                            }
                        }
                        catch (CryptographicException exception)
                        {
                            if (exception.Message == "Padding is invalid and cannot be removed.")
                                throw new ApplicationException("Universal Microsoft Cryptographic Exception (Not to be believed!)", exception);
                            else
                                throw;
                        }
                    }
                }
                /*using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, 
                        decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = 
                            new StreamReader(csDecrypt))
                        {
                            plainText = srDecrypt.ReadToEnd();
                        }
                    }
                }*/


            }

        }

        //todo not sure if that's what we meant (there is no entry generator value for instance)
        public static byte[] GenerateSessionKey(int keySize = 128)
        {
            byte[] sessionKey;
            using(Aes aes = Aes.Create())
            {
                aes.KeySize = keySize;
                aes.GenerateKey();
                sessionKey = (aes.Key).ToArray();
            }
            Console.WriteLine("dlugosc klucza "+ sessionKey.Length + " , klucz "+ sessionKey.ToString());
            return sessionKey;
        }


        //based on: https://stackoverflow.com/questions/1307204/how-to-generate-unique-public-and-private-key-via-rsa
        public static void GenerateKeyPairRSA(out string publicKey, out string privateKey)
        {
            using(var rsa = new RSACryptoServiceProvider(1024))
            {
                try
                {
                    //todo this creates some xml's, get key values from it or change everything
                    publicKey = rsa.ToXmlString(false);
                    privateKey = rsa.ToXmlString(true);

                    
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
        

        //TODO this is temporary
        //as a test- generates session key, encrypts and decrypts it
        public static void TestRSAEncrypt()
        {
            GenerateKeyPairRSA(out string publicKey, out string privateKey);
            byte[] sessionKey = GenerateSessionKey();
            Console.WriteLine("fresh key len: " + sessionKey.Length);
            
            string beforeEncryptionString = Convert.ToBase64String(sessionKey);
            Console.WriteLine("before encryption " + beforeEncryptionString);

            //encryption
            string encryptedKey = EncryptSessionKeyToString(sessionKey, publicKey);
            Console.WriteLine("encrypted data " + encryptedKey);

            //decryption
            byte[] decryptedData = DecryptSessionKeyFromString(encryptedKey, privateKey);
            Console.WriteLine("decrypted data " + Convert.ToBase64String(decryptedData) + " len: " + decryptedData.Length);
          
        }

        //returns encrypted session key with given public key (public key is a string in xml form)
        //encrypted key is returned as string
        public static string EncryptSessionKeyToString(byte[] sessionKey, string publicKey)
        {
            byte[] encryptedKeyBytes = RSAEncrypt(sessionKey, publicKey);
            string encryptedKeyString = Convert.ToBase64String(encryptedKeyBytes);

            return encryptedKeyString;
        }

        //given encrypted session key (a string from header of encrypted file) 
        //and a private key (string in xml form)
        //returns session key in byte array
        public static byte[] DecryptSessionKeyFromString(string encryptedKey, string privateKey)
        {
            byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKey);
            byte[] decryptedKeyBytes = RSADecrypt(encryptedKeyBytes, privateKey);
            return decryptedKeyBytes;
        }

        //based on: https://msdn.microsoft.com/en-us/library/system.security.cryptography.rsacryptoserviceprovider(v=vs.110).aspx
        public static byte[] RSAEncrypt(byte[] dataToEncrypt, string publicKey)
        {
            byte[] encryptedData = null;

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                try
                {
                    
                    rsa.FromXmlString(publicKey);
                    encryptedData = rsa.Encrypt(dataToEncrypt, false);

                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }

            return encryptedData;
        }

        //based on: https://msdn.microsoft.com/en-us/library/system.security.cryptography.rsacryptoserviceprovider(v=vs.110).aspx
        public static byte[] RSADecrypt(byte[] dataToDecrypt, string privateKey)
        {
            byte[] decryptedData = null;

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                try
                {

                    rsa.FromXmlString(privateKey);
                    decryptedData = rsa.Decrypt(dataToDecrypt, false);

                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }

            return decryptedData;
        }
    }
}
