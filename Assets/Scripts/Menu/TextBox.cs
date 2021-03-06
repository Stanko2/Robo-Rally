﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;



public class TextBox : MonoBehaviour
{
    public delegate void ValueChange (dynamic value);

    public event ValueChange OnChangeValue; 
    [System.Serializable]
    public enum TextBoxMode{String, Float, Int};
    public InputField inputField;
    [FormerlySerializedAs("DefaultValue")] public string defaultValue;
    public TextBoxMode textBoxMode;
    public string propertyName;
    public dynamic Value{
        get{
            switch (textBoxMode)
            {
                case TextBoxMode.Float:
                    return _floatValue;
                case TextBoxMode.Int:
                    return _intValue;
                case TextBoxMode.String:
                    return _stringValue;
                default:
                    return null;
            }
        }
    }
    int _intValue;
    string _stringValue;
    float  _floatValue;

    private void Start()
    {
        if (propertyName != String.Empty) GetComponentInChildren<Text>().text = propertyName;
        ValueChanged(defaultValue);
        inputField.text = defaultValue;
    }
    public void ValueChanged(string value){
        switch (textBoxMode)
        {
            case TextBoxMode.Float:
                float result0;
                if(float.TryParse(value, out result0)) _floatValue = result0;
                else
                { 
                    _floatValue = float.Parse(defaultValue);
                    inputField.text = defaultValue;
                }
                break;
            case TextBoxMode.Int:
                int result1;
                if(int.TryParse(value, out result1)) _intValue = result1;
                else
                { 
                    _intValue = int.Parse(defaultValue);
                    inputField.text = defaultValue;
                }
                break;
            case TextBoxMode.String:
                _stringValue = value;
                break;
            default:
                break;
        }
        OnChangeValue?.Invoke(Value);
    }
}
