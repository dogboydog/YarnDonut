#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SQLite;
using YarnSpinnerGodot;

/// <summary>
/// SQLite based custom variable storage example.
/// </summary>
public partial class SQLVariableStorage : VariableStorageBehaviour
{
    private SQLiteConnection db; // the database connector

    public override void _EnterTree()
    {
        // pick a place on disk for the database to save to
        string path = ProjectSettings.GlobalizePath("user://db.sqlite");
        // create a new database connection to speak to it
        db = new SQLiteConnection(path);
        // create the tables we need
        db.CreateTable<YarnString>();
        db.CreateTable<YarnFloat>();
        db.CreateTable<YarnBool>();
        GD.Print($"Initialized database at {path}");
    }

    public override bool TryGetValue<T>(string variableName, out T result)
    {
        string query = "";
        List<object> results = new();
        if (typeof(T) == typeof(IConvertible))
        {
            // we don't know the expected type
            if (TryGetValue<string>(variableName, out string stringResult))
            {
                result = (T) (object) stringResult;
                return true;
            }

            if (TryGetValue<float>(variableName, out float floatResult))
            {
                result = (T) (object) floatResult;
                return true;
            }

            if (TryGetValue<bool>(variableName, out bool boolResult))
            {
                result = (T) (object) boolResult;
                return true;
            }

            result = default(T);
            return false;
        }

        if (typeof(T) == typeof(string))
        {
            query = "SELECT * FROM YarnString WHERE key = ?";
            results.AddRange(db.Query<YarnString>(query, variableName));
        }
        else if (typeof(T) == typeof(bool))
        {
            query = "SELECT * FROM YarnBool WHERE key = ?";
            results.AddRange(db.Query<YarnBool>(query, variableName));
        }
        else if (typeof(T) == typeof(float))
        {
            
            query = "SELECT * FROM YarnFloat WHERE key = ?";
            results.AddRange(db.Query<YarnFloat>(query, variableName));
        }

        // if a result was found, convert it to type T and assign it
        if (results?.Count > 0)
        {
            if (results[0] is YarnFloat f)
            {
                result = (T) (object) f.value;
            }
            else if (results[0] is YarnBool b)
            {
                result = (T) (object) b.value;
            }
            else if (results[0] is YarnString s)
            {
                result = (T) (object) s.value;
            }
            else
            {
                throw new ArgumentException($"Unknown variable type {typeof(T)}");
            }

            return true;
        }

        // otherwise TryGetValue has failed
        result = default(T);
        return false;
    }

    public override void SetValue(string variableName, string stringValue)
    {
        // check it doesn't exist already in other table
        if (Exists(variableName, typeof(bool)))
        {
            throw new ArgumentException($"{variableName} is a bool.");
        }

        if (Exists(variableName, typeof(float)))
        {
            throw new ArgumentException($"{variableName} is a float.");
        }

        // if not, insert or update row in this table to the given value
        string query = "INSERT OR REPLACE INTO YarnString (key, value)";
        query += "VALUES (?, ?)";
        db.Execute(query, variableName, stringValue);
    }

    public override void SetValue(string variableName, float floatValue)
    {
        // check it doesn't exist already in other table
        if (Exists(variableName, typeof(string)))
        {
            throw new ArgumentException($"{variableName} is a string.");
        }

        if (Exists(variableName, typeof(bool)))
        {
            throw new ArgumentException($"{variableName} is a bool.");
        }

        // if not, insert or update row in this table to the given value
        string query = "INSERT OR REPLACE INTO YarnFloat (key, value)";
        query += "VALUES (?, ?)";
        db.Execute(query, variableName, floatValue);
    }

    public override void SetValue(string variableName, bool boolValue)
    {
        // check it doesn't exist already in other table
        if (Exists(variableName, typeof(string)))
        {
            throw new ArgumentException($"{variableName} is a string.");
        }

        if (Exists(variableName, typeof(float)))
        {
            throw new ArgumentException($"{variableName} is a float.");
        }

        // if not, insert or update row in this table to the given value
        string query = "INSERT OR REPLACE INTO YarnBool (key, value)";
        query += "VALUES (?, ?)";
        db.Execute(query, variableName, boolValue);
    }

    public override void Clear()
    {
        db.Execute("DELETE * FROM YarnString;");
        db.Execute("DELETE * FROM YarnBool;");
        db.Execute("DELETE * FROM YarnFloat;");
    }

    public override bool Contains(string variableName)
    {
        return Exists(variableName, typeof(string)) ||
               Exists(variableName, typeof(bool)) ||
               Exists(variableName, typeof(float));
    }

    public override void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings,
        Dictionary<string, bool> bools, bool clear = true)
    {
        foreach (var entry in floats)
        {
            SetValue(entry.Key, entry.Value);
        }

        foreach (var entry in bools)
        {
            SetValue(entry.Key, entry.Value);
        }

        foreach (var entry in strings)
        {
            SetValue(entry.Key, entry.Value);
        }
    }

    public override (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) GetAllVariables()
    {
        var floatQuery = "SELECT value FROM YarnFloat";
        var floats = db.Query<YarnFloat>(floatQuery)
            .AsEnumerable()
            .ToDictionary(f => f.key, f => f.value);
        var stringQuery = "SELECT value FROM YarnString";
        var strings = db.Query<YarnString>(stringQuery)
            .AsEnumerable()
            .ToDictionary(f => f.key, f => f.value);
        var boolQuery = "SELECT value FROM YarnBool";
        var booleans = db.Query<YarnBool>(boolQuery)
            .AsEnumerable()
            .ToDictionary(f => f.key, f => f.value);

        return (floats, strings, booleans);
    }

    private bool Exists(string variableName, Type type)
    {
        if (type == typeof(string))
        {
            string stringResult;
            if (TryGetValue(variableName, out stringResult))
            {
                return (stringResult != null);
            }
        }
        else if (type == typeof(bool))
        {
            if (TryGetValue(variableName, out bool _))
            {
                return true;
            }
        }
        else if (type == typeof(float))
        {
            if (TryGetValue(variableName, out float _))
            {
                return true;
            }
        }

        return false;
    }
}

public class YarnString
{
    [PrimaryKey] public string key { get; set; }
    public string value { get; set; }
}

public class YarnFloat
{
    [PrimaryKey] public string key { get; set; }
    public float value { get; set; }
}

public class YarnBool
{
    [PrimaryKey] public string key { get; set; }
    public bool value { get; set; }
}