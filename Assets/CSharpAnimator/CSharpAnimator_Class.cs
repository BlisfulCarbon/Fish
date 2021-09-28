#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

// ==================================================================
// ==================== CSharpAnimator_Class ========================
// ==================================================================
[System.Serializable]
public class SubTypeOfAnimatorType : SubTypeOf<CSharpAnimator_ClassBase> { }

public class CSharpAnimator_Class : CSharpAnimator, ISerializationCallbackReceiver
{
	[SerializeField, HideInInspector]
	public SubTypeOfAnimatorType selectedType = new SubTypeOfAnimatorType();
    [SerializeField, HideInInspector]
    public string selectedTypeData = null;
    [SerializeField, HideInInspector]
    public string lastSerializedTypeName = null;
    
	[NonSerialized]
	public Type lastTypeForOnValidate = null;
	[NonSerialized]
	public CSharpAnimator_ClassBase animatorType = null;

	[MenuItem("Assets/Create/CSharpAnimator/CSharpAnimator Class")]
	public static void Create()
	{
		CreateWithType(typeof(CSharpAnimator_Class));
	}
    
    public override void Module_EditorUpdate()
	{
        base.Module_EditorUpdate();
        
        if(animatorType == null)
        {
            LoadAnimatorType();
        }
	}

	public override void Module_CheckForPropertyUpdate()
	{
		base.Module_CheckForPropertyUpdate();

        Type type = selectedType.GetSelectedType();
		if(type != lastTypeForOnValidate)
		{
            LoadAnimatorType();
		}
	}

	public override bool Module_IsValidForGeneration()
	{
		bool result = selectedType.GetSelectedType() != null && IsTypeUpToDate();
		return result;
	}

	public override bool Module_Generate(AnimatorData outData)
	{
		outData = animatorType.ClearAndConstruct(outData);
        return outData != null;
	}

	public override void Module_DrawInspector(TextAnimatorEditor editor, SerializedObject serializedObject)
	{
		SerializedProperty selectedTypeProperty = serializedObject.FindProperty("selectedType");
		EditorGUILayout.PropertyField(selectedTypeProperty);
        
        Type type = selectedType.GetSelectedType();
        if(type != null)
        {
            if(IsTypeUpToDate())
            {
                if(animatorType != null)
                {
                    Editor typeEditor = Editor.CreateEditor(animatorType);
                    EditorGUI.BeginChangeCheck();
                    typeEditor.OnInspectorGUI();
                    bool changed = EditorGUI.EndChangeCheck();
                    if(changed)
                    {
                        EditorUtility.SetDirty(this);
                    }
                }    
            }
            else if(!string.IsNullOrEmpty(selectedType.selectedFullAssemblyType))
            {
                Color guiColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                GUILayout.Label("Selected type does not match serialized data.");
                GUI.backgroundColor = guiColor;
                
                if(GUILayout.Button("Apply new type"))
                {
                    lastSerializedTypeName = selectedType.selectedFullAssemblyType;
                }
            }
        }
	}
    
    public override void OnEnable()
    {
        LoadAnimatorType();
        base.OnEnable();
    }
    
    public void OnBeforeSerialize()
    {
        Type type = selectedType.GetSelectedType();
        if(animatorType != null && type != null && IsTypeUpToDate())
        {
            selectedTypeData = EditorJsonUtility.ToJson(animatorType);
            lastSerializedTypeName = selectedType.selectedFullAssemblyType;
        }
    }
    
    public void OnAfterDeserialize()
    {
    }
    
    public bool IsTypeUpToDate()
    {
        bool result = selectedType.selectedFullAssemblyType == lastSerializedTypeName;
        return result;
    }
    
    public void LoadAnimatorType()
    {
        if(IsTypeUpToDate())
        {
            Type type = selectedType.GetSelectedType();
            
            if(type != null)
            {
                animatorType = (CSharpAnimator_ClassBase)ScriptableObject.CreateInstance(type);
            }
            else
            {
                animatorType = null;
            }
            lastTypeForOnValidate = type;
            
            if(animatorType != null && selectedTypeData != null)
            {
                EditorJsonUtility .FromJsonOverwrite(selectedTypeData, animatorType);
            }
        }
        else
        {
            animatorType = null;
        }
    }
}

[System.Serializable]
public class CSharpAnimator_ClassBase : ScriptableObject
{
	public virtual AnimatorData Construct()
	{
		throw new Exception();
	}

	public AnimatorData ClearAndConstruct(AnimatorData outData)
	{
		nodeTable = new StateMachineNodeTable();
		currentData = outData;
		Construct();
		return currentData;
	}
    
    
// ==================================================================
// ========================= Code Interface =========================
// ==================================================================
	public const float DefaultExitTime = TransitionData.UnityCalculatedExitTime;
    public StateMachineNodeTable nodeTable;
	public AnimatorData currentData;
	public void Animator(GraphContext graph, TransitionGroupContext context)
	{
		AnimatorDataOps.Animator(graph, context);
	}
    public GraphContext Graph()
	{
		return AnimatorDataOps.Graph(currentData);
	}
	public TransitionGroupContext Transitions()
	{
		return AnimatorDataOps.Transitions(currentData);
	}
	public LayerContext Layer(string name)
	{
		return AnimatorDataOps.Layer(name);
	}
	public StateMachineContext StateMachine(string name)
	{
		return AnimatorDataOps.StateMachine(name, nodeTable);
	}
	public StateContext State(string name, Motion motion, float speed = 1.0f)
	{
		return AnimatorDataOps.State(name, motion, nodeTable, speed);
	}
	public BlendStateContext BlendState(string name, float speed = 1.0f)
	{
		return AnimatorDataOps.BlendState(name, nodeTable, speed);
	}
	public MotionContext BlendMotion1D(ParameterData paramX, bool automaticThresholds = true, string name = "")
	{
		return AnimatorDataOps.BlendMotion1D(paramX, automaticThresholds, name);
	}
    public MotionContext BlendMotion2DSimpleDirectional(ParameterData paramX, ParameterData paramY, string name = "")
	{
		return AnimatorDataOps.BlendMotion2DSimpleDirectional(paramX, paramY, name);
	}
    public MotionContext BlendMotion2DFreeFormDirectional(ParameterData paramX, ParameterData paramY, string name = "")
	{
		return AnimatorDataOps.BlendMotion2DFreeFormDirectional(paramX, paramY, name);
	}
    public MotionContext BlendMotion2DFreeFormCartesian(ParameterData paramX, ParameterData paramY, string name = "")
	{
		return AnimatorDataOps.BlendMotion2DFreeFormCartesian(paramX, paramY, name);
	}
	public MotionContext BlendMotionDirect(string name, bool normalisedBlendValues = false)
	{
		return AnimatorDataOps.BlendMotionDirect(name, normalisedBlendValues);
	}
	public MotionContext BlendClip1D(Motion motion, float threshold, float speed = 1.0f, bool mirror = false)
	{
		return AnimatorDataOps.BlendClip1D(motion, threshold, speed, mirror);
	}
    public MotionContext BlendClip2D(Motion motion, float thresholdX, float thresholdY, float speed = 1.0f, bool mirror = false)
	{
		return AnimatorDataOps.BlendClip2D(motion, thresholdX, thresholdY, speed, mirror);
	}
    public MotionContext BlendClipDirect(Motion motion, ParameterData param, float speed = 1.0f, bool mirror = false)
	{
		return AnimatorDataOps.BlendClipDirect(motion, param, speed, mirror);
	}
	public TransitionContext Transition(string name = null)
	{
		return AnimatorDataOps.Transition(nodeTable, name);
	}
    public AnimatorStateReference Exit()
    {
        AnimatorStateReference result = new AnimatorStateReference();
        result.type = AnimatorStateReferenceType.Exit;
        return result;
    }
    public TransitionContext Transition(AnimatorTransitionSource source, AnimatorStateReference destination, float exitTime, 
                                        float transitionTime, bool isFixedTransitionTime, TransitionInterruptionType type,
                                        params TOrTArray<TransitionConditionData>[] conditions)
    {
        return AnimatorDataOps.Transition(nodeTable, null, source, destination, exitTime, transitionTime, isFixedTransitionTime, type,
                                          conditions);
    }
    public TransitionContext Transition(AnimatorTransitionSource source, AnimatorStateReference destination, float exitTime, 
                                        TransitionInterruptionType type, params TOrTArray<TransitionConditionData>[] conditions)
    {
        return AnimatorDataOps.Transition(nodeTable, null, source, destination, exitTime, TransitionData.TransitionTimeDefault, TransitionData.IsTransitionTimeFixedDefault, 
                                          type, conditions);
    }
    public TransitionContext Transition(AnimatorTransitionSource source, AnimatorStateReference destination, float exitTime, 
                                        params TOrTArray<TransitionConditionData>[] conditions)
    {
        return AnimatorDataOps.Transition(nodeTable, null, source, destination, exitTime, TransitionData.TransitionTimeDefault, TransitionData.IsTransitionTimeFixedDefault,
                                          TransitionData.InterruptionTypeDefault, conditions);
    }
    public TransitionContext Transition(AnimatorTransitionSource source, AnimatorStateReference destination, params TOrTArray<TransitionConditionData>[] conditions)
    {
        return AnimatorDataOps.Transition(nodeTable, null, source, destination, TransitionData.ExitTimeDefault, TransitionData.TransitionTimeDefault, TransitionData.IsTransitionTimeFixedDefault, 
                                          TransitionData.InterruptionTypeDefault, conditions);
    }
    public AnimatorTransitionSource Any()
    {
        AnimatorStateReference state = new AnimatorStateReference();
        state.type = AnimatorStateReferenceType.Any;
        return new AnimatorTransitionSource(state, AnimatorTransitionSourceType.Single);
    }
    public AnimatorTransitionSource Single(string name)
    {
        return new AnimatorTransitionSource(name, AnimatorTransitionSourceType.Single);
    }
    public AnimatorTransitionSource Multiple(params AnimatorStateReference[] name)
    {
        return new AnimatorTransitionSource(name);
    }
    public AnimatorTransitionSource Recursive(string name)
    {
        return new AnimatorTransitionSource(name, AnimatorTransitionSourceType.Recursive);
    }
	public TransitionConditionData Condition(ParameterData parameter, ConditionMode mode, float threshold)
	{
		return AnimatorDataOps.Condition(parameter, mode, threshold);
	}
	public TransitionConditionData Bool(ParameterData parameter, bool value)
	{
		return AnimatorDataOps.Bool(parameter, value);
	}
	public TransitionConditionData Float(ParameterData parameter, float value, ConditionMode mode)
	{
		return AnimatorDataOps.Float(parameter, value, mode);
	}
	public TransitionConditionData Int(ParameterData parameter, int value, ConditionMode mode)
	{
		return AnimatorDataOps.Int(parameter, value, mode);
	}
	public TransitionConditionData Trigger(ParameterData parameter)
	{
		return AnimatorDataOps.Trigger(parameter);
	}
	public ParameterData NewParameter(AnimatorData animator, string name, ParameterType type)
	{
		return AnimatorDataOps.NewParameter(animator, name, type);
	}
	public ParameterData FloatParam(string name, float defaultValue = 0.0f)
	{
		return AnimatorDataOps.FloatParam(currentData, name, defaultValue);
	}
	public ParameterData BoolParam(string name, bool defaultValue = false)
	{
		return AnimatorDataOps.BoolParam(currentData, name, defaultValue);
	}
	public ParameterData IntParam(string name, int defaultValue = 0)
	{
		return AnimatorDataOps.IntParam(currentData, name, defaultValue);
	}
	public ParameterData TriggerParam(string name)
	{
		return AnimatorDataOps.TriggerParam(currentData, name);
	}
}

// NOTE(broscoe): Remove the script reference field. This will always be null for AnimatorType.
[CustomEditor(typeof(CSharpAnimator_ClassBase), true)]
public class AnimatorTypeEditor : Editor
{
    public static readonly string ignoredProperty = "m_Script";
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, ignoredProperty);
        serializedObject.ApplyModifiedProperties();
    }
}



// ==================================================================
// ======================= Type Serialisation =======================
// ==================================================================
[System.Serializable]
public class SubTypeOfSerializable
{
    public Type selectedType;

    [SerializeField, HideInInspector]
    public string selectedFullAssemblyType = "";

    [SerializeField, HideInInspector]
    public string parentFullAssemblyType = "";
}

[System.Serializable]
public class SubTypeOf<T> : SubTypeOfSerializable, ISerializationCallbackReceiver
{
	public SubTypeOf()
	{
		parentFullAssemblyType = typeof(T).AssemblyQualifiedName;
	}

    public void OnBeforeSerialize()
    {
        if(selectedType == null)
        {
            selectedFullAssemblyType = "";
        }
        else
        {
            selectedFullAssemblyType = selectedType.AssemblyQualifiedName;
        }
        parentFullAssemblyType = typeof(T).AssemblyQualifiedName;
    }

    public void OnAfterDeserialize()
    {
        if(!string.IsNullOrEmpty(selectedFullAssemblyType))
        {
            selectedType = Type.GetType(selectedFullAssemblyType);
        }
    }

	public Type GetSelectedType()
	{
		if(!string.IsNullOrEmpty(selectedFullAssemblyType) && selectedType == null)
        {
            selectedType = Type.GetType(selectedFullAssemblyType);
        }
        return selectedType;
	}
}

[CustomPropertyDrawer(typeof(SubTypeOfSerializable), true)]
public class SubClassOfPropertyDrawerer : PropertyDrawer
{
	public const string SelectedFullAssemblyType = "selectedFullAssemblyType";
    public const string ParentFullAssemblyType = "parentFullAssemblyType";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		SerializedProperty selectedTypeProp = property.FindPropertyRelative("selectedFullAssemblyType");
		SerializedProperty parentTypeProp = property.FindPropertyRelative("parentFullAssemblyType");

        string selectedTypeName = selectedTypeProp.stringValue;
        Type selectedType = string.IsNullOrEmpty(selectedTypeName) ? null : Type.GetType(selectedTypeName);

        string parentTypeName = parentTypeProp.stringValue;
        Type parentType = Type.GetType(parentTypeName);

		// TODO(broscoe): Cache these because they shouldn't change unless the assembly is reloaded!
		Type[] foundTypes = Assembly.GetAssembly(typeof(SubTypeOfSerializable)).GetTypes();
		Type[] typeOptions = new Type[foundTypes.Length];
		int optionCount = 0;
		for(int i = 0; i < typeOptions.Length; ++i)
		{
			Type type = foundTypes[i];
			if(type != parentType && parentType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
			{
				Assert.IsTrue(optionCount < typeOptions.Length);

				typeOptions[optionCount] = type;
				++optionCount;
			}
		}

		if(optionCount > 0)
		{
			int index = 0;

			string[] typeStrings = new string[optionCount];
			for(int i = 0; i < optionCount; ++i)
			{
				typeStrings[i] = typeOptions[i].FullName;
			}
			for(int i = 0; i < optionCount; ++i)
			{
				if(typeOptions[i] != null && typeOptions[i] == selectedType)
				{
					index = i;
					break;
				}
			}
			
			index = EditorGUILayout.Popup("Type:", index, typeStrings);
			selectedTypeProp.stringValue = typeOptions[index].AssemblyQualifiedName;
		}
		else
		{
			EditorGUILayout.LabelField(string.Format("No sub types of {0} found!", parentTypeName));
		}
    }
}
#endif