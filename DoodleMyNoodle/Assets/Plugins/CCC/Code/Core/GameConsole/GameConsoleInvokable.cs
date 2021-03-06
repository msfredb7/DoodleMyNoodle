using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngineX;

public abstract class ConsoleInvokableAttribute : Attribute
{
    /// <summary>
    /// The display name of the command or var. Case insensitive when invoked by user.
    /// </summary>
    public string Name;

    /// <summary>
    /// The description of the command of var.
    /// </summary>
    public string Description;

    /// <summary>
    /// If set to false, the command will not be usable until manually enabled using <see cref="GameConsole.SetCommandOrVarEnabled"/>.
    /// </summary>
    public bool EnabledByDefault = true;

    /// <summary>
    /// If set, this command or var will only be usable when the group is enabled using <see cref="GameConsole.SetGroupEnabled(string, bool)"/>. Case insensitive when invoked by user.
    /// </summary>
    public string EnableGroup = null;

    /// <summary>
    /// If the command or var is used while deactivated, should we buffer the executing for when it is re-enabled?
    /// </summary>
    public bool BufferWhileInactive = true;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class ConsoleVarAttribute : ConsoleInvokableAttribute
{
    public enum SaveMode
    {
        NotSaved,
        PlayerPrefs
    }

    public SaveMode Save = SaveMode.NotSaved;

    public ConsoleVarAttribute() { }

    public ConsoleVarAttribute(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConsoleCommandAttribute : ConsoleInvokableAttribute
{
    public ConsoleCommandAttribute() { }

    public ConsoleCommandAttribute(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

public interface IGameConsoleInvokable
{
    string DisplayName { get; }
    string Description { get; }
    Type ReturnType { get; }
    ReadOnlyListDynamic<IGameConsoleParameter> Parameters { get; }
    bool Enabled { get; }
}

public interface IGameConsoleParameter
{
    string Name { get; }
    Type Type { get; }
    bool Optional { get; }
    bool HasDefaultValue { get; }
    object DefaultValue { get; }
}

internal abstract class GameConsoleInvokable : IGameConsoleInvokable
{
    public class Parameter : IGameConsoleParameter
    {
        public readonly string Name;
        public readonly Type Type;
        public readonly bool Optional;
        public readonly bool HasDefaultValue;
        public readonly object DefaultValue;

        public Parameter(string name, Type type, bool optional, bool hasDefaultValue, object defaultValue)
        {
            Name = name;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Optional = optional;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        string IGameConsoleParameter.Name => Name;
        Type IGameConsoleParameter.Type => Type;
        bool IGameConsoleParameter.Optional => Optional;
        bool IGameConsoleParameter.HasDefaultValue => HasDefaultValue;
        object IGameConsoleParameter.DefaultValue => DefaultValue;
    }

    public bool EnabledSelf = true;
    public string Name { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public Type ReturnType { get; protected set; }
    public List<Parameter> InvokeParameters { get; protected set; }
    public int MandatoryParameterCount { get; protected set; }
    public string EnableGroup { get; protected set; }
    public bool BufferWhileInactive { get; protected set; }

    string IGameConsoleInvokable.DisplayName => DisplayName;
    string IGameConsoleInvokable.Description => Description;
    Type IGameConsoleInvokable.ReturnType => ReturnType;
    ReadOnlyListDynamic<IGameConsoleParameter> IGameConsoleInvokable.Parameters => InvokeParameters.AsReadOnlyNoAlloc().DynamicCast<IGameConsoleParameter>();
    bool IGameConsoleInvokable.Enabled => EnabledSelf;


    protected void Construct(MemberInfo memberInfo, ConsoleInvokableAttribute attribute, List<Parameter> parameters)
    {
        string displayName;
        if (string.IsNullOrEmpty(attribute.Name))
        {
            displayName = memberInfo.Name;
        }
        else
        {
            displayName = attribute.Name;
        }
        
        EnabledSelf = attribute.EnabledByDefault;
        EnableGroup = attribute.EnableGroup?.ToLower();
        BufferWhileInactive = attribute.BufferWhileInactive;
        DisplayName = displayName;
        Name = DisplayName.ToLower();
        Description = attribute.Description;
        InvokeParameters = parameters;
        MandatoryParameterCount = InvokeParameters.Count((p) => !p.Optional);
    }

    public virtual void Init() { }

    public Type GetFirstUnsupportedParameter()
    {
        foreach (var param in InvokeParameters)
        {
            if (!GameConsoleParser.IsSupportedParameterType(param.Type))
            {
                return param.Type;
            }
        }
        return null;
    }

    public bool ConflictsWith(GameConsoleInvokable other)
    {
        // For now, identical name => conflict!
        // If needed, we could develop a more complex analysis and allow for same-name methods but with different parameters
        if (string.Compare(other.Name, Name, ignoreCase: true) == 0)
            return true;
        return false;
        //int maxManadatoryParamCount = Mathf.Max(MandatoryParameterCount, other.MandatoryParameterCount);
        //for (int i = 0; i < maxManadatoryParamCount; i++)
        //{
        //    if(i >= Parameters.Length)
        //    {
        //        return false;
        //    }

        //    if(i >= other.Parameters.Length)
        //    {
        //        return false;
        //    }

        //    if(Parameters[i].Type !=)
        //}
    }

    public abstract void Invoke(string[] paramStrings);
}

internal abstract class GameConsoleFieldOrProperty : GameConsoleInvokable
{
    private ConsoleVarAttribute.SaveMode _saveMode = ConsoleVarAttribute.SaveMode.NotSaved;
    private string _prefsKeys;
    private readonly Type _memberType;

    public GameConsoleFieldOrProperty(MemberInfo memberInfo, Type memberType)
    {
        _memberType = memberType;
        ConsoleVarAttribute attribute = memberInfo.GetCustomAttribute<ConsoleVarAttribute>();

        var invokeParameters = new List<Parameter>()
        {
            new Parameter(null, memberType, optional: true, hasDefaultValue: false, null)
        };

        _saveMode = attribute.Save;

        Construct(memberInfo, attribute, invokeParameters);
    }

    public override void Init()
    {
        base.Init();

        if (_saveMode == ConsoleVarAttribute.SaveMode.PlayerPrefs)
        {
            if (PlayerPrefsX.SupportedValueTypes.Contains(_memberType))
            {
                if (CanGetValue() && CanSetValue())
                {
                    _saveMode = ConsoleVarAttribute.SaveMode.PlayerPrefs;
                    _prefsKeys = "unity-x-gameconsole-var-" + DisplayName;
                }
                else
                {
                    Log.Error($"ConsoleVar {DisplayName} cannot be saved in preferences because it needs to be 'gettable' and 'settable'.");
                }
            }
            else
            {
                Log.Error($"ConsoleVar {DisplayName} cannot be saved in preferences because it is of an unsupported save type.");
            }
        }

        if (_saveMode != ConsoleVarAttribute.SaveMode.NotSaved)
        {
            if (TryLoadValue(out object value))
                SetValue(value);
        }
    }

    public override void Invoke(string[] paramStrings)
    {
        if (paramStrings.Length == 1)
        {
            if (GameConsoleParser.Parse(paramStrings[0], InvokeParameters[0].Type, out object value))
            {
                if (CanSetValue())
                {
                    SetValue(value);

                    if (_saveMode != ConsoleVarAttribute.SaveMode.NotSaved)
                        SaveValue(value);
                }
                else
                {
                    Log.Warning($"Cannot set {DisplayName}'s value.");
                }
            }
            else
            {
                Log.Warning($"Type {InvokeParameters[0].Type.GetPrettyName()} expected.");
            }
        }
        else if (paramStrings.Length == 0)
        {
            if (CanGetValue())
            {
                GameConsole.Write($"{DisplayName} = {GetValue() ?? "null"}", GameConsole.LineColor.Command);
            }
            else
            {
                Log.Warning($"Cannot get {DisplayName}'s value.");
            }
        }
        else
        {
            Log.Warning($"Expecting 0 or 1 parameter.");
        }
    }

    void SaveValue(object value)
    {
        switch (_saveMode)
        {
            case ConsoleVarAttribute.SaveMode.PlayerPrefs:
                PlayerPrefsX.Set(_prefsKeys, value);
                PlayerPrefs.Save();
                break;
        }

    }

    bool TryLoadValue(out object value)
    {
        switch (_saveMode)
        {
            case ConsoleVarAttribute.SaveMode.PlayerPrefs:
                if (PlayerPrefs.HasKey(_prefsKeys))
                {
                    return PlayerPrefsX.TryGet(_prefsKeys, _memberType, default, out value);
                }
                break;
        }
        value = default;
        return false;
    }

    protected abstract bool CanGetValue();
    protected abstract bool CanSetValue();
    protected abstract object GetValue();
    protected abstract void SetValue(object value);
}

internal class GameConsoleProperty : GameConsoleFieldOrProperty
{
    private readonly PropertyInfo _propertyInfo;

    public GameConsoleProperty(PropertyInfo propertyInfo)
        : base(propertyInfo, propertyInfo.PropertyType)
    {
        _propertyInfo = propertyInfo;
    }

    protected override bool CanGetValue()
    {
        return _propertyInfo.CanRead;
    }

    protected override bool CanSetValue()
    {
        return _propertyInfo.CanWrite;
    }

    protected override object GetValue()
    {
        return _propertyInfo.GetValue(null);
    }

    protected override void SetValue(object value)
    {
        _propertyInfo.SetValue(null, value);
    }
}

internal class GameConsoleField : GameConsoleFieldOrProperty
{
    private readonly FieldInfo _fieldInfo;

    public GameConsoleField(FieldInfo fieldInfo)
        : base(fieldInfo, fieldInfo.FieldType)
    {
        _fieldInfo = fieldInfo;
    }

    protected override bool CanGetValue()
    {
        return true;
    }

    protected override bool CanSetValue()
    {
        return !_fieldInfo.IsInitOnly;
    }

    protected override object GetValue()
    {
        return _fieldInfo.GetValue(null);
    }

    protected override void SetValue(object value)
    {
        _fieldInfo.SetValue(null, value);
    }
}


internal class GameConsoleCommand : GameConsoleInvokable
{
    private readonly MethodInfo _methodInfo;

    public GameConsoleCommand(MethodInfo methodInfo)
    {
        _methodInfo = methodInfo;
        ConsoleCommandAttribute attribute = methodInfo.GetCustomAttribute<ConsoleCommandAttribute>();
        List<Parameter> parameters = new List<Parameter>();
        foreach (ParameterInfo paramInfo in methodInfo.GetParameters())
        {
            parameters.Add(new Parameter(paramInfo.Name, paramInfo.ParameterType, paramInfo.HasDefaultValue, paramInfo.HasDefaultValue, paramInfo.DefaultValue));
        }

        Construct(methodInfo, attribute, parameters);
    }

    public override void Invoke(string[] paramStrings)
    {
        object[] paramObjs = new object[InvokeParameters.Count];

        // fill default values
        for (int i = 0; i < InvokeParameters.Count; i++)
        {
            if (InvokeParameters[i].Optional)
            {
                paramObjs[i] = InvokeParameters[i].DefaultValue;
            }
        }

        if (paramStrings.Length > InvokeParameters.Count)
        {
            Log.Warning($"The command {DisplayName} does not take {paramStrings.Length} arguments. It takes {InvokeParameters.Count}.");
            return;
        }

        if (paramStrings.Length < MandatoryParameterCount)
        {
            Log.Warning($"The command {DisplayName} needs at least {MandatoryParameterCount} parameters.");
            return;
        }

        for (int i = 0; i < paramStrings.Length; i++)
        {
            if (!GameConsoleParser.Parse(paramStrings[i], InvokeParameters[i].Type, out paramObjs[i]))
            {
                Log.Warning($"The {ToPositionNumber(i + 1)} parameter ({InvokeParameters[i].Name}) is expected to be of type '{InvokeParameters[i].Type.GetPrettyName()}'");
                return;
            }
        }

        _methodInfo.Invoke(null, paramObjs);
    }

    private string ToPositionNumber(int position)
    {
        if (position == 1)
            return "1st";
        if (position == 2)
            return "2nd";
        if (position == 3)
            return "3rd";
        return position + "th";
    }
}