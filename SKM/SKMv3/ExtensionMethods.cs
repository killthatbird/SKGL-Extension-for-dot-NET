﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SKM.V3.Models;
using SKM.V3.Internal;
using System.Security.Cryptography;

namespace SKM.V3
{
    /// <summary>
    /// The extension methods are not thought to be used through this class, but instead
    /// through the relevant objects that they affect. Please see the examples below.
    /// </summary>
    /// <example>
    /// <code language="csharp" title="Checking if a license key has feature 1 set to true and that it has not expired.">
    /// // assuming license is a LicenseKey object.
    /// if(license.HasFeature(1)
    ///           .HasNotExpired()
    ///           .IsValid())
    /// {
    ///     // do something
    /// }
    /// else
    /// {
    ///     // invalid license.
    /// }
    /// </code>
    /// </example>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Saves the current <see cref="LicenseKey"/> object to file.
        /// </summary>
        /// <returns>Returns the original object if successful. Null otherwise.</returns>
        public static LicenseKey SaveToFile(this LicenseKey licenseKey)
        {
            string name = "licensekey.skm";
            return SaveToFile(licenseKey, name);
        }
        /// <summary>
        /// Saves the current <see cref="LicenseKey"/> object to file.
        /// </summary>
        /// <param name="file">The entire path including file name, i.e. c:\folder\file.txt</param>
        /// <returns>Returns the original object if successful. Null otherwise.</returns>
        public static LicenseKey SaveToFile(this LicenseKey licenseKey, string file)
        {
            System.IO.StreamWriter sw = null;
            bool state = false;
            try
            {
                sw = new System.IO.StreamWriter(file);
                sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(licenseKey));
                state = true;
            }
            catch
            {
                state = false;

            }
            finally
            {
                if (sw != null)
                    sw.Dispose();
            }

            return state ? licenseKey : null;
        }

        /// <summary>
        /// Loads the <see cref="LicenseKey"/> object from a file.
        /// </summary>
        /// <remarks>The current object will not be affected. Instead, 
        /// you need to assign it manually, eg. licenseKey = licenseKey.LoadFromFile().</remarks>
        /// <returns>Returns the original object if successful. Null otherwise.</returns>
        public static LicenseKey LoadFromFile(this LicenseKey licenseKey)
        {
            string name = "licensekey.skm";
            return LoadFromFile(licenseKey, name);
        }

        /// <summary>
        /// Loads the <see cref="LicenseKey"/> object from a file.
        /// </summary>
        /// <param name="file">The entire path including file name, i.e. c:\folder\file.txt</param>
        /// <remarks>The current object will not be affected. Instead, 
        /// you need to assign it manually, eg. licenseKey = licenseKey.LoadFromFile().</remarks>
        /// <returns>Returns the original object if successful. Null otherwise.</returns>
        public static LicenseKey LoadFromFile(this LicenseKey licenseKey, string file)
        {
            System.IO.StreamReader sr = null;
            LicenseKey ki = null;
            try
            {
                sr = new System.IO.StreamReader(file);
                ki = Newtonsoft.Json.JsonConvert.DeserializeObject<LicenseKey>(sr.ReadToEnd());
            }
            catch { }
            finally
            {
                if (sr != null)
                    sr.Dispose();
            }

            return ki;
        }

        /// <summary>
        /// Checks if the license key file has been modified by an unauthorized person.
        /// This method will only return true if the license key information is exactly
        /// the same as the one that was provided and signed by SKM.
        /// </summary>
        /// <param name="rsaPublicKey"></param>
        /// <returns>Returns true if the signature is valid. False otherwise.</returns>
        private static bool IsLicenceseKeyGenuine(this LicenseKey licenseKey, string rsaPublicKey)
        {
            if (licenseKey?.Signature != "")
            {
                var prevSignature = licenseKey.Signature;

                try
                {
                    licenseKey.Signature = "";
                    var rawResult = licenseKey.AsDictionary();
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);

                    rsa.FromXmlString(rsaPublicKey);

                    byte[] signature = Convert.FromBase64String(prevSignature);

                    // the signature should not be included into the signature :)
                    System.Diagnostics.Debug.WriteLine(String.Join(",", rawResult.Select(x => x.Value)));
                    return rsa.VerifyData(HelperMethods.GetBytes(String.Join(",", rawResult.Select(x => x.Value))), "SHA256", signature);
                }
                catch { }
                finally
                {
                    licenseKey.Signature = prevSignature;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks that the Key Information object is valid (in the correct format). You can always add constraints
        /// such as @<see cref="HasNotExpired"/>.
        /// </summary>
        /// <returns>Returns true if the object is valid and false otherwise.</returns>
        public static bool IsValid(this LicenseKey licenseKey)
        {
            if (licenseKey != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks that the Key Information object is valid (in the correct format). You can always add constraints
        /// such as @<see cref="HasNotExpired"/>.
        /// <br></br>
        /// It will also check if the license key file has been modified by an unauthorized person.
        /// This method will only return true if the license key information is exactly
        /// the same as the one that was provided and signed by SKM.
        /// </summary>
        /// <param name="rsaPublicKey">The public key (RSA). It can be found here: https://serialkeymanager.com/User/Security </param>
        /// <returns>Returns true if the object is valid and false otherwise.</returns>
        public static bool IsValid(this LicenseKey licenseKey, string rsaPublicKey)
        {
            if (licenseKey != null)
            {
                if (!IsLicenceseKeyGenuine(licenseKey, rsaPublicKey))
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks that they key has not expired (i.e. the expire date has not been reached).
        /// </summary>
        /// <param name="checkWithInternetTime">If set to true, we will also check that the local
        /// time (on the client computer) has not been changed (using SKM.TimeCheck). 
        /// </param>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey HasNotExpired(this LicenseKey licenseKey, bool checkWithInternetTime = false)
        {
            if (licenseKey != null)
            {
                TimeSpan ts = licenseKey.Expires - DateTime.Today;

                if (ts.Days >= 0)
                {
                    if (checkWithInternetTime && SKGL.SKM.TimeCheck())
                        return null;
                    return licenseKey;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks that this object has a valid signature, which means that the content has not been altered
        /// after that it was generated by Serial Key Manager.
        /// </summary>
        /// <param name="rsaPublicKey">The public key (RSA). It can be found here: https://serialkeymanager.com/User/Security </param>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey HasValidSignature(this LicenseKey licenseKey, string rsaPublicKey)
        {
            return HasValidSignature(licenseKey, rsaPublicKey, null);
        }


        /// <summary>
        /// Checks that this object has a valid signature, which means that the content has not been altered
        /// after that it was generated by Serial Key Manager.
        /// </summary>
        /// <param name="rsaPublicKey">The public key (RSA). It can be found here: https://serialkeymanager.com/User/Security </param>
        /// <param name="signatureExpirationInterval">If the license key was signed,
        /// this method will check so that no more than "signatureExpirationInterval" days have passed since the last activation.
        /// </param>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey HasValidSignature(this LicenseKey licenseKey, string rsaPublicKey, int? signatureExpirationInterval)
        {
            if (licenseKey != null)
            {
                if (IsLicenceseKeyGenuine(licenseKey, rsaPublicKey))
                {
                    if (signatureExpirationInterval.HasValue)
                    {
                        TimeSpan ts = DateTime.Today - licenseKey.SignDate;
                        if (ts.Days >= signatureExpirationInterval.Value)
                        {
                            return null;
                        }
                    }

                    return licenseKey;
                }
            }
            return null;
        }


        /// <summary>
        /// Checks so that a certain Feature is enabled (i.e. it's set to TRUE).
        /// </summary>
        /// <param name="featureNumber">The feature number, eg. feature1, feature 2, etc. FeatureNumber can be 1,2,...,8.</param>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey HasFeature(this LicenseKey licenseKey, int featureNumber)
        {
            if (licenseKey != null && featureNumber <= 8
                                       && featureNumber >= 1
                                       && GetFeatureByNumber(licenseKey, featureNumber))
            {
                return licenseKey;
            }
            return null;
        }

        /// <summary>
        /// Checks so that a certain Feature is disabled (i.e. it's set to FALSE).
        /// </summary>
        /// <param name="featureNumber">The feature number, eg. feature1, feature 2, etc. FeatureNumber can be 1,2,...,8.</param>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey HasNotFeature(this LicenseKey licenseKey, int featureNumber)
        {
            if (licenseKey != null && featureNumber <= 8
                                       && featureNumber >= 1
                                       && !GetFeatureByNumber(licenseKey, featureNumber))
            {
                return licenseKey;
            }
            return null;
        }

        /// <summary>
        /// Checks so that the machine code corresponds to the machine code of this computer.
        /// The default hash function is SHA1.
        /// </summary>
        /// <returns></returns>
        public static LicenseKey IsOnRightMachine(this LicenseKey licenseKey)
        {
            return IsOnRightMachine(licenseKey, SKGL.SKM.getSHA1);
        }

        /// <summary>
        /// Checks so that the machine code corresponds to the machine code of this computer.
        /// </summary>
        /// <param name="hashFunction">A hash function used to hash the current computer's parameters.</param>
        /// <returns></returns>
        public static LicenseKey IsOnRightMachine(this LicenseKey licenseKey, Func<string, string> hashFunction)
        {
            if (licenseKey != null && licenseKey.ActivatedMachines != null)
            {
                var mc = SKGL.SKM.getMachineCode(hashFunction);

                foreach (var machine in licenseKey.ActivatedMachines.Where(x=> x.Mid != null))
                {
                    // if we find a machine code that corresponds to that of this machine -> success.
                    if (machine.Mid.Equals(mc)) return licenseKey;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks so that a they key is blocked.
        /// </summary>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey IsBlocked(this LicenseKey licenseKey)
        {
            if (licenseKey != null && licenseKey.Block)
            {
                return licenseKey;
            }
            return null;
        }

        /// <summary>
        /// Checks so that a they key is not blocked.
        /// </summary>
        /// <returns>A key information object if the condition is satisfied. Null otherwise.</returns>
        public static LicenseKey IsNotBlocked(this LicenseKey licenseKey)
        {
            if (licenseKey != null && !licenseKey.Block)
            {
                return licenseKey;
            }
            return null;
        }


        /// <summary>
        /// Checks if the list contains an object with a specific name.
        /// </summary>
        /// <param name="name">The name of the data object</param>
        /// <returns>True if the data object with the specific name exists and false otherwise.</returns>
        public static bool Contains(this List<DataObject> dataObjects, string name)
        {
            if (dataObjects != null)
            {
                foreach (var obj in dataObjects)
                {
                    if(obj.Name == name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a data object with the specified name.
        /// </summary>
        /// <param name="name">The name of the data object</param>
        /// <returns>The data object if the data object with the specific name exists and null otherwise.</returns>
        public static DataObject Get(this List<DataObject> dataObjects, string name)
        {
            if (dataObjects != null)
            {
                foreach (var obj in dataObjects)
                {
                    if (obj.Name == name)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }


        internal static bool GetFeatureByNumber(this LicenseKey licenseKey, int i)
        {
            switch (i)
            {
                case 1:
                    return licenseKey.F1;
                case 2:
                    return licenseKey.F2;
                case 3:
                    return licenseKey.F3;
                case 4:
                    return licenseKey.F4;
                case 5:
                    return licenseKey.F5;
                case 6:
                    return licenseKey.F6;
                case 7:
                    return licenseKey.F7;
                case 8:
                    return licenseKey.F8;
                default:
                    return false;
            }
        }

        internal static void SetFeatureByNumber(this LicenseKey licenseKey, int i, bool value)
        {
            switch (i)
            {
                case 1:
                    licenseKey.F1 = value;
                    break;
                case 2:
                     licenseKey.F2 = value;
                    break;
                case 3:
                     licenseKey.F3 = value;
                    break;
                case 4:
                     licenseKey.F4 = value;
                    break;
                case 5:
                     licenseKey.F5 = value;
                    break;
                case 6:
                     licenseKey.F6 = value;
                    break;
                case 7:
                     licenseKey.F7=value;
                    break;
                case 8:
                     licenseKey.F8 = value;
                    break;
            }
        }
    }
}
