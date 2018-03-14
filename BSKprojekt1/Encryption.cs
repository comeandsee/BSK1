﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BSKprojekt1
{
    public static class Encryption
    {
        //based on https://msdn.microsoft.com/pl-pl/library/system.security.cryptography.aes(v=vs.110).aspx
        //encrypts file from path srcFileName to file in path destFileName
        public static void EncryptFromFile(string srcFileName, string destFileName, 
            byte[] key, CipherMode mode, int blockSize, out byte[] IV)
        {
            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();
                aesAlg.Mode = mode;
                aesAlg.BlockSize = blockSize;

                IV = (aesAlg.IV).ToArray();
                
                
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();

                //based on: https://stackoverflow.com/questions/9237324/encrypting-decrypting-large-files-net
                using (FileStream destFileStream = new FileStream(destFileName, FileMode.CreateNew, 
                    FileAccess.Write, FileShare.None))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(destFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (FileStream source = new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            source.CopyTo(cryptoStream);
                        }
                    }
                }


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

        public static void DecryptToFile(string encodedFileName, string decodedFileName, byte[] key, CipherMode mode, int blockSize, byte[] IV)
        {
            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.Mode = mode;
                aesAlg.BlockSize = blockSize;
                aesAlg.IV = IV;

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
        public static byte[] GenerateSessionKey()
        {
            byte[] sessionKey;
            using(Aes aes = Aes.Create())
            {
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

                    
                    //File.WriteAllText("C:\\Users\\Zbigniew\\Desktop\\keyz2.xml", privateKey);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        //TODO- getting the key from publicKeys.xml file (that doesn't exist yet)
        //gets public key of user with userEmail
        //returns it as xml string
        public static string GetUsersPublicKey(string userEmail)
        {/*
            XmlDocument doc = new XmlDocument();
            doc.Load(Globals.UsersXmlFilePath);

            XmlNode usersNode = doc.DocumentElement.
                SelectSingleNode("//" + Globals.UsersNode);

            if (usersNode == null)
            {
                Console.WriteLine("there is no users node");
                return;
            }

            //for each user
            foreach (XmlNode node in usersNode.ChildNodes)
            {
                userEmail = node[Globals.XmlEmail];
                user = new User(userEmail.InnerText);
                users.Add(user);
                Console.WriteLine("added " + user.Email);

            }*/
            string userPublicKey = null;
            return userPublicKey;
        }


        public static void TestRSAEncrypt()
        {
            GenerateKeyPairRSA(out string publicKey, out string privateKey);

            byte[] dataToEncrypt = Encoding.ASCII.GetBytes("dzien dobry misiu");

            //encryption
            byte[] encryptedData = RSAEncrypt(dataToEncrypt, publicKey);
            Console.WriteLine("encrypted data " + Encoding.Default.GetString(encryptedData));

            //decryption
            byte[] decryptedData = RSADecrypt(encryptedData, privateKey);
            Console.WriteLine("decrypted data " + Encoding.Default.GetString(decryptedData));


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