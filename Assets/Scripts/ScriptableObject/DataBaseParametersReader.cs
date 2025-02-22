﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataBaseParametersReader : MonoBehaviour
{

    private static DataBaseParametersReader instance;
    public static DataBaseParametersReader Instance { get { return instance; } }

    private void Awake()
    {
        instance = this;
        Base.isBaseInitialized = true;
    }

    [SerializeField]
    private DataBaseParameters parameters;
    public DataBaseParameters Parameters { get { return parameters; } }


    override public string ToString()
    {
        string s = "";
        foreach (var field in Parameters.GetType().GetFields())
        {
            s += field.Name + "= " + field.GetValue(parameters) + "\n";
        }
        return s;
    }

   
}
