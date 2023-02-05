using System;
using System.Collections.Generic;
using System.IO;
using Hkmp.Util;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth;

/// <summary>
/// Generic authentication list containing a set of authentication keys.
/// </summary>
internal class AuthKeyList {
    /// <summary>
    /// The name of the file that stores the keys.
    /// </summary>
    protected string FileName { get; set; }

    /// <summary>
    /// Set of approved authentication keys.
    /// </summary>
    [JsonProperty("approved")] private readonly HashSet<string> _approved;

    /// <summary>
    /// Construct the auth key list.
    /// </summary>
    protected AuthKeyList() {
        _approved = new HashSet<string>();
    }

    /// <summary>
    /// Whether a given key is contained in this list.
    /// </summary>
    /// <param name="authKey">The authentication key to check.</param>
    /// <returns>true if the key is contained in this list, false otherwise.</returns>
    public bool Contains(string authKey) {
        return _approved.Contains(authKey);
    }

    /// <summary>
    /// Add the given auth key to the list.
    /// </summary>
    /// <param name="authKey">The authentication key to add.</param>
    public void Add(string authKey) {
        _approved.Add(authKey);

        WriteToFile();
    }

    /// <summary>
    /// Remove the given auth key from the list.
    /// </summary>
    /// <param name="authKey">The authentication key to remove.</param>
    public void Remove(string authKey) {
        _approved.Remove(authKey);

        WriteToFile();
    }

    /// <summary>
    /// Remove all authentication keys from the list.
    /// </summary>
    public void Clear() {
        _approved.Clear();

        WriteToFile();
    }

    /// <summary>
    /// Write this authentication key list to a file.
    /// </summary>
    protected void WriteToFile() {
        FileUtil.WriteObjectToJsonFile(
            this,
            Path.Combine(FileUtil.GetCurrentPath(), FileName)
        );
    }

    /// <summary>
    /// Load an auth key list form file with the given path. Create a new
    /// instance if the path does not point to an existing file.
    /// </summary>
    /// <param name="fileName">The name of the file to load.</param>
    /// <returns>The loaded or fresh instance of the class.</returns>
    public static AuthKeyList LoadFromFile(string fileName) {
        return LoadFromFile(
            () => new AuthKeyList { FileName = fileName },
            fileName
        );
    }

    /// <summary>
    /// Load a class extending AuthKeyList from file with the given path. Create a new instance
    /// if the path does not point to an existing file. This will call the given function to
    /// instantiate it.
    /// </summary>
    /// <param name="instantiator">The function instantiating a new instance.</param>
    /// <param name="fileName">The name of the file to load.</param>
    /// <typeparam name="T">The type of the auth key list.</typeparam>
    /// <returns>The loaded or fresh instance of the class.</returns>
    protected static T LoadFromFile<T>(Func<T> instantiator, string fileName) where T : AuthKeyList {
        if (File.Exists(fileName)) {
            var loaded = FileUtil.LoadObjectFromJsonFile<T>(
                Path.Combine(FileUtil.GetCurrentPath(), fileName)
            );

            if (loaded != null) {
                loaded.FileName = fileName;
                return loaded;
            }
        }

        var authKeyList = instantiator.Invoke();
        authKeyList.WriteToFile();
        return authKeyList;
    }
}
