//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using UnityEngine;

namespace Microsoft.Unity
{
    /// <summary>
    /// An attribute that can be used to designate where a secret value is held.
    /// </summary>
    /// <remarks>
    /// This attribute is used by <see cref="SecretHelper"/>. Please see that class for usage.
    /// </remarks>
    [AttributeUsage(validOn: AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class SecretValueAttribute : Attribute
    {
        #region Member Variables
        private string name;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="SecretValueAttribute"/> instance.
        /// </summary>
        /// <param name="name">
        /// The name of the secret value.
        /// </param>
        public SecretValueAttribute(string name)
        {
            // Validate
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(nameof(name));

            // Store
            this.name = name;
        }
        #endregion // Constructors

        #region Public Properties
        /// <summary>
        /// Gets the name of the secret.
        /// </summary>
        public string Name => name;
        #endregion // Public Properties
    }

    /// <summary>
    /// A class built to help keep API keys and other secret values out of public source control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To use <see cref="SecretHelper"/>, apply the <see cref="SecretValueAttribute">SecretValue</see>
    /// attribute to any inspector fields that you would like kept secret. Next, place the values in the
    /// corresponding environment variable. Finally, in your behavior's <c>Awake</c> or <c>Start</c> method, call
    /// <see cref="SecretHelper.LoadSecrets(object)">SecretHelper.LoadSecrets(this)</see>.
    /// </para>
    /// <para>
    /// For an example of using <see cref="SecretHelper"/>, please see <see cref="Microsoft.Unity.LUIS.LuisManager">LuisManager</see>.
    /// </para>
    /// <para>
    /// <b>IMPORTANT:</b> Please be aware that Unity Editor only loads environment variables once on start.
    /// You will need to close Unity and open it again for changes to environment variables to take effect.
    /// Also, Unity Hub acts as a parent process when starting Unity from the Hub. Therefore you will need
    /// to close not only Unity but also Unity Hub (which runs in the tray) before changes will take effect.
    /// </para>
    /// </remarks>
    static public class SecretHelper
    {
        #region Internal Methods
        /// <summary>
        /// Gets the default value for a specified type.
        /// </summary>
        /// <param name="t">
        /// The type to obtain the default for.
        /// </param>
        /// <returns>
        /// The default value for the type.
        /// </returns>
        static private object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to load a secret value into the specified field.
        /// </summary>
        /// <param name="sva">
        /// A <see cref="SecretValueAttribute"/> that indicates the source of the secret value.
        /// </param>
        /// <param name="field">
        /// The field where the value will be loaded.
        /// </param>
        /// <param name="obj">
        /// The object instance where the value will be set.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> to overwrite non-default values; otherwise <c>false</c>. The default is <c>false</c>.
        /// </param>
        /// <remarks>
        /// By default <see cref="SecretHelper"/> will only update fields that are set to default values
        /// (e.g. 0 for int and null or "" for string). This allows values set in the Unity inspector to
        /// override values stored in the environment. If values in the environment should always take
        /// precedence over values stored in the field set <paramref name="overwrite"/> to <c>true</c>.
        /// </remarks>
        static private void TryLoadValue(SecretValueAttribute sva, FieldInfo field, object obj, bool overwrite = false)
        {
            // Validate
            if (sva == null) throw new ArgumentNullException(nameof(sva));
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            // Now get the current value of the field
            object curValue = field.GetValue(obj);

            // If we're not overwriting values, we need to check to check and make sure a non-default value is not already set
            if (!overwrite)
            {
                // What is the default value for the field?
                object defValue = GetDefaultValue(field.FieldType);

                // Is it the current value the same as the default value?
                bool isDefaultValue = ((curValue == defValue) || ((field.FieldType == typeof(string)) && (string.IsNullOrEmpty((string)curValue))));

                // If the current value is not the default value, the secret has already been supplied
                // and we don't need to do any more work.
                if (!isDefaultValue) { return; }
            }

            // Either in overwrite mode or a default value. Let's try to read the environment variable.
            string svalue = Environment.GetEnvironmentVariable(sva.Name);

            // Check for no environment variable or no value set.
            if (string.IsNullOrEmpty(svalue))
            {
                Debug.LogWarning($"{obj.GetType().Name}.{field.Name} has the default value '{curValue}' but the environment variable {sva.Name} is missing or not set.");
                return;
            }

            // If string, just assign. Otherwise attempt to convert.
            if (field.FieldType == typeof(string))
            {
                field.SetValue(obj, svalue);
            }
            else
            {
                try
                {
                    object cvalue = Convert.ChangeType(svalue, field.FieldType);
                    field.SetValue(obj, cvalue);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"The value '{svalue}' of environment variable {sva.Name} could not be converted to {field.FieldType.Name}. {ex.Message}");
                }
            }
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <summary>
        /// Attempts to load all secret values for the specified object.
        /// </summary>
        /// <param name="obj">
        /// The object where secret values will be loaded.
        /// </param>
        static public void LoadSecrets(object obj)
        {
            // Validate
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            // Get all fields
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            // Look for secret fields
            foreach (var field in fields)
            {
                // Try to get attribute
                SecretValueAttribute sva = field.GetCustomAttribute<SecretValueAttribute>();

                // If not a secret, skip
                if (sva == null) { continue; }

                // Try to load the value
                TryLoadValue(sva, field, obj);
            }
        }
        #endregion // Public Methods
    }
}