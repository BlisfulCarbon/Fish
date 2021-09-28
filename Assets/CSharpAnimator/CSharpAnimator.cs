#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Assertions;

// ==================================================================
// ========================= Text Animator ==========================
// ==================================================================
public struct AnimatorStateWithParent
{
    public AnimatorState state;
    public AnimatorStateMachine parent;

    public bool IsValid()
    {
        return state != null;
    }
}
public struct AnimatorStateMachineWithParent
{
    public AnimatorStateMachine machine;
    public AnimatorStateMachine parent;

    public bool IsValid()
    {
        return machine != null;
    }
}
public class AnimatorStateTable : Dictionary<int, AnimatorStateWithParent> { }
public class AnimatorMachineTable : Dictionary<int, AnimatorStateMachineWithParent> { }
public class AnimatorPositionTable : Dictionary<int, AnimatorNodePosition> { }
public class StateMachineNodeTable : Dictionary<int, StateMachineNodeData>
{
	public static void AddNodeToTable(StateMachineNodeTable table, StateMachineNodeData node)
	{
		Assert.IsTrue(table != null);

        Assert.IsTrue(!String.IsNullOrEmpty(node.name));
		int hash = UnityEngine.Animator.StringToHash(node.name);

		StateMachineNodeData unused;
		bool found = table.TryGetValue(hash, out unused);
		Assert.IsTrue(!found, string.Format("Found name conflict: {0} when generating animator data. All names must be unique!", node.name));

		if(!found)
		{ 
			table.Add(hash, node);
		}
	}
}

public struct AnimatorNodePosition
{
	public Vector2 parentPosition;
	public Vector2 position;
	public Vector2 anyPosition;
	public Vector2 entryPosition;
	public Vector2 exitPosition;
}

public enum StateMachineNodeType
{
	State = 0,
	Machine,
}
public class StateMachineNodeData
{
	public StateMachineNodeType type;
	public string name;
    public string tag;

	// NOTE(broscoe): State data.
	public MotionData motion;
    public bool ikOnFeet;
    public bool writeDefaultValues = true;
    
    // NOTE(broscoe): If the param is null, we'll use the value instead.
	public float speed = 1.0f;
    public ParameterData speedParam;
    
    public ParameterData normalisedTimeParam;
    
    public bool mirror;
    public ParameterData mirrorParam;
    
    public float cycleOffset;
    public ParameterData cycleOffsetParam;

    
	// NOTE(broscoe): StateMachine data.
	public List<StateMachineNodeData> nodes = new List<StateMachineNodeData>();
    public string defaultStateName;
}

public enum LayerBlendingMode
{
    Override,
    Additive
}
public class LayerData
{
	public string name;
	public StateMachineNodeData rootNode;
    
    public float weight = 1.0f;
    public AvatarMask mask;
    
    public bool ikPass;
    public bool timing;
    
    public string syncLayerName;
    public LayerBlendingMode blendingMode;

	public void CreateRootStateMachine()
	{
		rootNode = new StateMachineNodeData();
		rootNode.type = StateMachineNodeType.Machine;
		SetRootStateMachineName();
	}
    
    public void SetRootStateMachineName()
    {
        rootNode.name = string.Format("Root_{0}", name);
    }
}

public enum ParameterType
{
	Float = 0,
	Int,
	Bool,
	Trigger
}
public class ParameterData
{
	public string name;
	public ParameterType type;

	public bool defaultBoolValue;
	public float defaultFloatValue;
	public int defaultIntValue;
}

public enum ConditionMode
{
	If = 0,
	IfNot,
	Greater,
	Less,
	Equals,
	NotEqual
}
public class TransitionConditionData
{
	public ConditionMode mode;
	public ParameterData parameter;
	public float threshold;
}

public enum TransitionInterruptionType
{
	None = 0,
	Source = 1,
	Destination = 2,
	SourceThenDestination = 3,
	DestinationThenSource = 4
}
public class TransitionData
{
	// NOTE(broscoe): -1.0f for no exit time.
	public const float NoExitTime = -1.0f;
	public const float UnityCalculatedExitTime = -10.0f;
    
    // NOTE(broscoe): Let Unity calculate our transition time.
    public const float TransitionTimeDefault = -1.0f;   
    public const float ExitTimeDefault = NoExitTime;
    public const TransitionInterruptionType InterruptionTypeDefault = TransitionInterruptionType.Destination;
    public const bool IsTransitionTimeFixedDefault = true;

	public string name;
	public AnimatorStateReference[] sources;
	public int sourceCount;
	public AnimatorStateReference destination;

	public List<TransitionConditionData> conditions = new List<TransitionConditionData>();

	public float exitTime = ExitTimeDefault;
	public float transitionTime = TransitionTimeDefault;
	public bool isTransitionTimeFixed = IsTransitionTimeFixedDefault;
	public float transitionOffset;
	// NOTE(broscoe): I think this provides a better default.
	public TransitionInterruptionType interruptionType = InterruptionTypeDefault;
	public bool orderedInterruption = true;
}

public enum MotionDataType
{
	Clip = 0,
	BlendTree
}
public enum TreeType
{
    BlendTree1D,
    BlendTree2D_SimpleDirectional,
    BlendTree2D_FreeFormDirectional,
    BlendTree2D_FreeFormCartesian,
    BlendTreeDirect,
}
public class MotionData
{
    public MotionDataType type;
    public float thresholdX;
    public float thresholdY;
    
	// NOTE(broscoe): Clip parameters.
	public Motion clip;
    public float speed = 1.0f;
    public bool mirror;
    public ParameterData directParam;
    
	// NOTE(broscoe): Blend motion parameters.
    public TreeType blendTreeType;
	public string name;
	public List<MotionData> blendMotions = new List<MotionData>();
	public ParameterData paramX;
	public ParameterData paramY;
    
    // NOTE(broscoe): 1D
    public bool automaticThresholds = true;
    
    // NOTE(broscoe): 2D*
    // TODO(broscoe): Compute positions
    
    // NOTE(broscoe): Direct
    public bool normalisedBlendValues;
    
	public MotionData(MotionDataType type)
	{
		this.type = type;
	}
}

public class AnimatorData
{
	public List<LayerData> layers = new List<LayerData>();
	public List<TransitionData> transitions = new List<TransitionData>();
	public List<ParameterData> parameters = new List<ParameterData>();
}


// ==================================================================
// ===================== Base Scriptable Object =====================
// ==================================================================

// NOTE(broscoe): This must be called the same name as this file due to a limitation in Unity.
public class CSharpAnimator : ScriptableObject
{
#if UNITY_EDITOR    
	public static void CreateWithType(Type type)
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if(path == "")
		{
			path = Application.dataPath;
		}
		else if(Path.GetExtension(path) != "")
		{
			path = Path.GetDirectoryName(path);
		}
		path += string.Format("/{0}.controller", type.Name);
		path = AssetDatabase.GenerateUniqueAssetPath(path);
        
		AnimatorController controller = AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorController)) as AnimatorController;
		if(controller == null)
		{
			controller = AnimatorController.CreateAnimatorControllerAtPath(path);
			// NOTE(broscoe): Remove the default layer Unity creates.
			controller.RemoveLayer(0);
            
			AssetDatabase.SaveAssets();
            
			CSharpAnimator animator = (CSharpAnimator)ScriptableObject.CreateInstance(type);
			animator.hideFlags |= HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(animator, controller);
            
			AssetDatabase.SaveAssets();
		}
	}
    
	public virtual void OnEnable()
	{
		Module_CheckForPropertyUpdate();
		EditorApplication.update += EditorUpdate;
	}
    
	public void OnDisable()
	{
		EditorApplication.update -= EditorUpdate;
	}
    
	public void OnDestroy()
	{
		EditorApplication.update -= EditorUpdate;
	}
    
	public void EditorUpdate()
	{
		Module_EditorUpdate();
	}
    
	public virtual void Module_EditorUpdate()
	{
	}
    
	public virtual void Module_CheckForPropertyUpdate()
	{
	}
    
	public void OnValidate()
	{
		Module_CheckForPropertyUpdate();
	}
    
	public bool IsValidForGeneration()
	{
		bool result = false;
		result = Module_IsValidForGeneration();
		return result;
	}
    
	public void GenerateInto(AnimatorController controller)
	{
		if(IsValidForGeneration())
		{
			AnimatorData data = new AnimatorData();
            
			bool success = Module_Generate(data);
            
			if(success && AnimatorDataOps.IsValidForGeneration(data))
			{
				AnimatorGenerator.GenerateAnimator(data, controller);
			}
		}
	}
#endif

	public virtual bool Module_IsValidForGeneration()
	{
		return true;
	}

	public virtual bool Module_Generate(AnimatorData outData)
	{
		nodeTable = new StateMachineNodeTable();
		currentData = outData;
		outData = Construct();
        return outData != null;
	}
    
    public virtual void Module_DrawInspector(TextAnimatorEditor editor, SerializedObject serializedObject)
	{
        editor.DrawBaseInspector();
	}

	public virtual AnimatorData Construct()
	{
		return null;
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
    public AnimatorTransitionSource Single(AnimatorStateReference name)
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

public enum AnimatorStateReferenceType
{
    Normal,
    Any,
    Exit,
}
public struct AnimatorStateReference
{
    public AnimatorStateReferenceType type;
    public string name;

    public static implicit operator AnimatorStateReference(string n)
    {
        AnimatorStateReference result;
        result.type = AnimatorStateReferenceType.Normal;
        result.name = n;
        return result;
    }

    public static AnimatorStateReference AnyState()
    {
        AnimatorStateReference result;
        result.type = AnimatorStateReferenceType.Any;
        result.name = null;
        return result;
    }

    public static AnimatorStateReference ExitState()
    {
        AnimatorStateReference result;
        result.type = AnimatorStateReferenceType.Exit;
        result.name = null;
        return result;
    }
}

public enum AnimatorTransitionSourceType
{
    Single,
    Multiple,
    Recursive,
}
public struct AnimatorTransitionSource
{
    public TOrTArray<AnimatorStateReference> source;
    public AnimatorTransitionSourceType type;
    
    public AnimatorTransitionSource(AnimatorStateReference state, AnimatorTransitionSourceType type)
    {
        this.source = state;
        this.type = type;
    }
    
    public AnimatorTransitionSource(AnimatorStateReference[] states)
    {
        this.source = states;
        this.type = AnimatorTransitionSourceType.Multiple;
    }
}
public class AnimatorDataOps
{
    public const int MinTransitions = 8;
    
    public static int GetLayerIndexFromName(AnimatorData data, string name)
    {
        int result = -1;
        for(int i = 0; i < data.layers.Count; ++i)
        {
            if(data.layers[i].name == name)
            {
                result = i;
                break;
            }
        }
        return result;
    }
    
	public static bool IsValidForGeneration(AnimatorData data)
	{
		bool result = data != null && data.layers != null && data.layers.Count > 0;
		return result;
	}
    
	public static void AddParameter(AnimatorData animatorData, ParameterData data)
	{
		animatorData.parameters.Add(data);
	}
    
	public static LayerData NewLayer(string name)
	{
		LayerData layer = new LayerData();
		layer.name = name;
		layer.CreateRootStateMachine();
		return layer;
	}
    
	public static StateMachineNodeData NewState(string name, Motion motion, StateMachineNodeTable nodeTable, 
												float speed = 1.0f)
	{
		StateMachineNodeData newNode = new StateMachineNodeData();
		newNode.type = StateMachineNodeType.State;
		newNode.name = name;
		newNode.motion = new MotionData(MotionDataType.Clip);
		newNode.motion.clip = motion;
		newNode.speed = speed;
        
		StateMachineNodeTable.AddNodeToTable(nodeTable, newNode);
		return newNode;
	}
    
	public static StateMachineNodeData NewBlendState(string name, StateMachineNodeTable nodeTable, float speed = 1.0f)
	{
		StateMachineNodeData newNode = new StateMachineNodeData();
		newNode.type = StateMachineNodeType.State;
		newNode.name = name;
		newNode.speed = speed;
        
		StateMachineNodeTable.AddNodeToTable(nodeTable, newNode);
		return newNode;
	}
    
	public static MotionData NewBlendMotion1D(ParameterData paramX, bool automaticThresholds, string name)
	{
		MotionData data = new MotionData(MotionDataType.BlendTree);
        data.blendTreeType = TreeType.BlendTree1D;
		data.name = name;
		data.paramX = paramX;
        data.automaticThresholds = automaticThresholds;
		return data;
	}
    
    public static MotionData NewBlendMotion2D(ParameterData paramX, ParameterData paramY, string name, TreeType type)
    {
        MotionData data = new MotionData(MotionDataType.BlendTree);
        data.blendTreeType = type;
        data.name = name;
		data.paramX = paramX;
        data.paramY = paramY;
        return data; 
    }
    
    public static MotionData NewBlendMotion2DSimpleDirectional(ParameterData paramX, ParameterData paramY, string name)
    {
        MotionData data = NewBlendMotion2D(paramX, paramY, name, TreeType.BlendTree2D_SimpleDirectional);
        return data;
    }
    
    public static MotionData NewBlendMotion2DFreeFormDirectional(ParameterData paramX, ParameterData paramY, string name)
    {
        MotionData data = NewBlendMotion2D(paramX, paramY, name, TreeType.BlendTree2D_FreeFormDirectional);
        return data;
    }
    
    public static MotionData NewBlendMotion2DFreeFormCartesian(ParameterData paramX, ParameterData paramY, string name)
    {
        MotionData data = NewBlendMotion2D(paramX, paramY, name, TreeType.BlendTree2D_FreeFormCartesian);
        return data;
    }
    
    public static MotionData NewBlendMotionDirect(string name, bool normalisedBlendValues)
    {
        MotionData data = new MotionData(MotionDataType.BlendTree);
        data.blendTreeType = TreeType.BlendTreeDirect;
        data.name = name;
        data.normalisedBlendValues = normalisedBlendValues;
        return data;
    }
    
    public static MotionData NewBlendClipDirect(Motion motion, ParameterData param, float speed, bool mirror)
    {
        MotionData data = new MotionData(MotionDataType.Clip);
        data.clip = motion;
        data.speed = speed;
        data.mirror = mirror;
        data.directParam = param;
		return data;
    }
    
	public static MotionData NewBlendClip1D(Motion motion, float threshold, float speed, bool mirror)
	{
		MotionData data = new MotionData(MotionDataType.Clip);
        data.clip = motion;
		data.thresholdX = threshold;
        data.speed = speed;
        data.mirror = mirror;
		return data;
	}
    
    public static MotionData NewBlendClip2D(Motion motion, float thresholdX, float thresholdY, float speed, bool mirror)
    {
        MotionData data = new MotionData(MotionDataType.Clip);
        data.clip = motion;
		data.thresholdX = thresholdX;
        data.thresholdY = thresholdY;
        data.speed = speed;
        data.mirror = mirror;
		return data;
    }
    
	public static StateMachineNodeData NewStateMachine(string name, StateMachineNodeTable nodeTable)
	{
		StateMachineNodeData newNode = new StateMachineNodeData();
		newNode.type = StateMachineNodeType.Machine;
		newNode.name = name;
        
		StateMachineNodeTable.AddNodeToTable(nodeTable, newNode);
		return newNode;
	}
    
	public static TransitionData NewTransition(string name = null)
	{
		TransitionData data = new TransitionData();
		data.name = name;
		return data;
	}
    
	public static void Animator(GraphContext graph, TransitionGroupContext context)
	{
	}
    
	public static void AllocateAndMerge<T, ContextT>(TOrTArray<ContextT>[] arrays, GetFromContextDelegate<T, ContextT> getFromContext, List<T> list)
	{        
		for(int i = 0; i < arrays.Length; ++i)
		{
			switch(arrays[i].type)
			{
				case TOrTArrayType.Single:
				{
					list.Add(getFromContext(arrays[i].value));
				}
				break;
                
				case TOrTArrayType.Array:
				{
					if(arrays[i].valueArray != null)
					{
						for(int a = 0; a < arrays[i].valueArray.Length; ++a)
						{
							list.Add(getFromContext(arrays[i].valueArray[a]));
						}
					}
				}
				break;
			}
		}
	}
    
	// Commmon code interface:
	public static GraphContext Graph(AnimatorData currentData)
	{
		Assert.IsTrue(currentData != null);
        
		GraphContext context = new GraphContext();
		context.data = currentData;
		return context;
	}
	
	public static TransitionGroupContext Transitions(AnimatorData currentData)
	{
		Assert.IsTrue(currentData != null);
        
		TransitionGroupContext context = new TransitionGroupContext();
		context.data = currentData;
		return context;
	}
	
	public static LayerContext Layer(string name)
	{
		LayerContext context = new LayerContext();
		context.data = AnimatorDataOps.NewLayer(name);
		return context;
	}
    
	public static StateMachineContext StateMachine(string name, StateMachineNodeTable nodeTable)
	{
		StateMachineContext context = new StateMachineContext();
		context.data = AnimatorDataOps.NewStateMachine(name, nodeTable);
		return context;
	}
    
	public static StateContext State(string name, Motion motion, StateMachineNodeTable nodeTable, float speed = 1.0f)
	{
		StateContext context = new StateContext();
		context.data = AnimatorDataOps.NewState(name, motion, nodeTable, speed);
		return context;
	}
    
	public static BlendStateContext BlendState(string name, StateMachineNodeTable nodeTable, float speed = 1.0f)
	{
		BlendStateContext context = new BlendStateContext();
		context.data = AnimatorDataOps.NewBlendState(name, nodeTable, speed);
		return context;
	}
    
	public static MotionContext BlendMotion1D(ParameterData paramX, bool automaticThresholds = true, string name = "")
	{
		MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendMotion1D(paramX, automaticThresholds, name);
		return context;
	}
    
    public static MotionContext BlendMotion2DSimpleDirectional(ParameterData paramX, ParameterData paramY, string name = "")
	{
		MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendMotion2DSimpleDirectional(paramX, paramY, name);
		return context;
	}
    
    public static MotionContext BlendMotion2DFreeFormDirectional(ParameterData paramX, ParameterData paramY, string name = "")
	{
		MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendMotion2DFreeFormDirectional(paramX, paramY, name);
		return context;
	}
    
    public static MotionContext BlendMotion2DFreeFormCartesian(ParameterData paramX, ParameterData paramY, string name = "")
	{
		MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendMotion2DFreeFormCartesian(paramX, paramY, name);
		return context;
	}
    
    public static MotionContext BlendMotionDirect(string name, bool normalisedBlendValues)
    {
        MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendMotionDirect(name, normalisedBlendValues);
		return context;
    }
    
    public static MotionContext BlendClipDirect(Motion motion, ParameterData param, float speed = 1.0f, bool mirror = false)
    {
        MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendClipDirect(motion, param, speed, mirror);
		return context;
    }
    
	public static MotionContext BlendClip1D(Motion motion, float threshold, float speed = 1.0f, bool mirror = false)
	{
		MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendClip1D(motion, threshold, speed, mirror);
		return context;
	}
    
    public static MotionContext BlendClip2D(Motion motion, float thresholdX, float thresholdY, float speed, bool mirror)
    {
        MotionContext context = new MotionContext();
		context.data = AnimatorDataOps.NewBlendClip2D(motion, thresholdX, thresholdY, speed, mirror);
		return context;
    }
    
	public static TransitionContext Transition(StateMachineNodeTable nodeTable, string name = null)
	{
		Assert.IsTrue(nodeTable != null);
        
		TransitionContext context = new TransitionContext();
		context.data = AnimatorDataOps.NewTransition(name);
		context.nodeTable = nodeTable;
		return context;
	}
    
    public static TransitionContext Transition(StateMachineNodeTable nodeTable, string name, AnimatorTransitionSource source, 
                                               AnimatorStateReference destination, float exitTime, float transitionTime, bool isFixedTransitionTime, 
                                               TransitionInterruptionType type, TOrTArray<TransitionConditionData>[] conditions)
    {
        TransitionContext context = Transition(nodeTable, name);
        context.Destination(destination);
        context.ExitTime(exitTime);
        context.TransitionTime(transitionTime);
        context.FixedTransitionTime(isFixedTransitionTime);
        context.Interruption(type);
        context.SetConditions(conditions);
        
        switch(source.type)
        {
            case AnimatorTransitionSourceType.Single: context.Source(source.source.value); break;
            case AnimatorTransitionSourceType.Recursive: context.SourceRecursive(source.source.value); break;
            case AnimatorTransitionSourceType.Multiple: context.SourceMultiple(source.source.valueArray); break;
        }
        
        return context;
    }
    
	public static TransitionConditionData Condition(ParameterData parameter, ConditionMode mode, float threshold)
	{
		TransitionConditionData data = new TransitionConditionData();
		data.mode = mode;
		data.parameter = parameter;
		data.threshold = threshold;
		return data;
	}
    
	public static TransitionConditionData Bool(ParameterData parameter, bool value)
	{
		return Condition(parameter, value ? ConditionMode.If : ConditionMode.IfNot, 0.0f);
	}
    
	public static TransitionConditionData Float(ParameterData parameter, float value, ConditionMode mode)
	{
		return Condition(parameter, mode, value);
	}
    
	public static TransitionConditionData Int(ParameterData parameter, int value, ConditionMode mode)
	{
		return Condition(parameter, mode, value);
	}
    
	public static TransitionConditionData Trigger(ParameterData parameter)
	{
		return Condition(parameter, ConditionMode.If, 0.0f);
	}
    
	public static ParameterData NewParameter(AnimatorData animator, string name, ParameterType type)
	{
		ParameterData data = new ParameterData();
		data.name = name;
		data.type = type;
		
		AnimatorDataOps.AddParameter(animator, data);
        
		return data;
	}
    
	public static ParameterData FloatParam(AnimatorData currentData, string name, float defaultValue = 0.0f)
	{
		ParameterData data = NewParameter(currentData, name, ParameterType.Float);
		data.defaultFloatValue = defaultValue;
		return data;
	}
    
	public static ParameterData BoolParam(AnimatorData currentData, string name, bool defaultValue = false)
	{
		ParameterData data = NewParameter(currentData, name, ParameterType.Bool);
		data.defaultBoolValue = defaultValue;
		return data;
	}
    
	public static ParameterData IntParam(AnimatorData currentData, string name, int defaultValue = 0)
	{
		ParameterData data = NewParameter(currentData, name, ParameterType.Int);
		data.defaultIntValue = defaultValue;
		return data;
	}
    
	public static ParameterData TriggerParam(AnimatorData currentData, string name)
	{
		ParameterData data = NewParameter(currentData, name, ParameterType.Trigger);
		return data;
	}
    
    public static void ResizeTransitionsSources(TransitionData data, int sizeToAdd)
    {
        if(data.sources == null)
        {
            data.sources = new AnimatorStateReference[sizeToAdd > MinTransitions ? sizeToAdd : MinTransitions];
        }
        else
        {
            int currentSize = data.sources.Length;
            if(currentSize <= 0)
            {
                currentSize = MinTransitions;
            } 
            int newSize = currentSize + sizeToAdd;
            
            while(currentSize < newSize)
            {
                currentSize *= 2;
            }
            
            AnimatorStateReference[] newData = new AnimatorStateReference[newSize];
            for(int i = 0; i < data.sources.Length; ++i)
            {
                newData[i] = data.sources[i];
            }
        }
    }
    
    public static void AddSources(TransitionData data, AnimatorStateReference name)
    {
        ResizeTransitionsSources(data, 1);
        data.sources[data.sourceCount] = name;
        ++data.sourceCount;
    }
    
    public static void AddSources(TransitionData data, AnimatorStateReference[] names)
    {
        int size = names.Length;
        
        ResizeTransitionsSources(data, size);
        for(int i = 0; i < size; ++i)
        {
            data.sources[data.sourceCount] = names[i];
            ++data.sourceCount;
        }
    }
    
	public static void AddSourcesRecursive(TransitionData data, AnimatorStateReference name, StateMachineNodeTable nodeTable)
	{
		Assert.IsTrue(nodeTable != null);
        Assert.IsTrue(name.type == AnimatorStateReferenceType.Normal, "Any, Enter, and Exit states cannot be used with Recursive.");

		int hash = UnityEngine.Animator.StringToHash(name.name);
		StateMachineNodeData node;
		nodeTable.TryGetValue(hash, out node);
        
		if(node == null)
		{
			Debug.LogWarningFormat("Recursive transition source {0} was not found.", name);
		}
		else
		{
			int count = 0;
			CountSourceStatesRecursive(node, ref count);
            ResizeTransitionsSources(data, count);
			PushSourceStatesRecursive(node, data.sources, ref data.sourceCount);
		}
	}
    
	public static void CountSourceStatesRecursive(StateMachineNodeData node, ref int count)
	{
		if(node.type == StateMachineNodeType.State)
		{
			++count;
		}
		else
		{
			if(node.nodes != null)
			{
				for(int i = 0; i < node.nodes.Count; ++i)
				{
					StateMachineNodeData child = node.nodes[i];
					if(child.type == StateMachineNodeType.State)
					{
						++count;
					}
					else
					{
						CountSourceStatesRecursive(child, ref count);
					}
				}
			}
		}
	}
    
	public static void PushSourceStatesRecursive(StateMachineNodeData node, AnimatorStateReference[] outSources, ref int index)
	{
		if(node.type == StateMachineNodeType.State)
		{
			Assert.IsTrue(outSources.Length > index);
			outSources[index] = node.name;
			++index;
		}
		else
		{
			if(node.nodes != null)
			{
				for(int i = 0; i < node.nodes.Count; ++i)
				{
					StateMachineNodeData child = node.nodes[i];
					if(child.type == StateMachineNodeType.State)
					{
						Assert.IsTrue(outSources.Length > index);
						outSources[index] = child.name;
						++index;
					}
					else
					{
						PushSourceStatesRecursive(child, outSources, ref index);
					}
				}
			}
		}
	}
}


// ==================================================================
// ========================= Code Interface =========================
// ==================================================================
public enum VCT
{
    Single,
    Multiple,
}
public class AnimatorNodeInputAttribute : System.Attribute
{
    public VCT countType;
    public bool required;
    public int typeGroup;
    public AnimatorNodeInputAttribute(VCT countType, int typeGroup, bool required)
    {
        this.typeGroup = typeGroup;
        this.countType = countType;
        this.required = required;
    }
}

public delegate T GetFromContextDelegate<T, ContextT>(ContextT context);
public enum TOrTArrayType
{
	Single = 0,
	Array,
}
public struct TOrTArray<T>
{
	public T value;
	public T[] valueArray;
	public TOrTArrayType type;
    
	public static implicit operator TOrTArray<T>(T v)
	{
		TOrTArray<T> result = new TOrTArray<T>();
		result.value = v;
		result.type = TOrTArrayType.Single;
		return result;
	}
    
	public static implicit operator TOrTArray<T>(T[] v)
	{
		TOrTArray<T> result = new TOrTArray<T>();
		result.valueArray = v;
		result.type = TOrTArrayType.Array;
		return result;
	}
}

// NOTE(broscoe): We generate memory garbage with these types, but the type safety is probably worth it.
public class StateMachineNodeContext
{
	public StateMachineNodeData data;
}

public class StateContext : StateMachineNodeContext
{
    public const int GT_IKOnFeet = 1 << 0;
    public const int GT_WriteDefaultValues = 1 << 1;
    public const int GT_Speed = 1 << 2;
    public const int GT_SpeedParam = 1 << 3;
    public const int GT_NormalisedTimeParam = 1 << 4;
    public const int GT_Mirror = 1 << 5;
    public const int GT_MirrorParam = 1 << 6;
    public const int GT_CycleOffset = 1 << 7;
    public const int GT_CycleOffsetParam = 1 << 8;
    public const int GT_Motion = 1 << 9;
    public const int GT_Tag = 1 << 10;
    public const int GT_Name = 1 << 11;
    
    [AnimatorNodeInput(VCT.Single, GT_IKOnFeet, false)]
    public StateContext IKOnFeet(bool v)
    {
        data.ikOnFeet = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_WriteDefaultValues, false)]
    public StateContext WriteDefaultValues(bool v)
    {
        data.writeDefaultValues = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Speed, false)]
    public StateContext Speed(float t)
	{
		data.speed = t;
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_SpeedParam, false)]
    public StateContext SpeedParam(ParameterData param)
    {
        data.speedParam = param;
        return this;
    }

#if false
	[AnimatorNodeInput(VCT.Single, GT_NormalisedTimeParam, false)]
    public StateContext NormalisedTimeParam(ParameterData param)
    {
        data.normalisedTimeParam = param;
        return this;
    }
#endif
    
    [AnimatorNodeInput(VCT.Single, GT_Mirror, false)]
    public StateContext Mirror(bool v)
    {
        data.mirror = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_MirrorParam, false)]
    public StateContext MirrorParam(ParameterData param)
    {
        data.mirrorParam = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_CycleOffset, false)]
    public StateContext CycleOffset(float v) 
    {
        data.cycleOffset = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_CycleOffsetParam, false)]
    public StateContext CycleOffsetParam(ParameterData param)
    {
        data.cycleOffsetParam = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Motion, true)]
    public StateContext Motion(Motion motion)
	{
		data.motion = new MotionData(MotionDataType.Clip);
		data.motion.clip = motion;
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_Tag, false)]
    public StateContext Tag(string tag)
    {
        data.tag = tag;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Name, true)]
    public StateContext Name(string name)
    {
        data.name = name;
        return this;
    }
}

public class BlendStateContext : StateContext
{
    new public BlendStateContext IKOnFeet(bool v)
    {
        base.IKOnFeet(v);
        return this;
    }
    
    new public BlendStateContext WriteDefaultValues(bool v)
    {
        base.WriteDefaultValues(v);
        return this;
    }
    
	new public BlendStateContext Speed(float t)
	{
        base.Speed(t);
		return this;
	}
    
    new public BlendStateContext SpeedParam(ParameterData param)
    {
        base.SpeedParam(param);
        return this;
    }

#if false
	new public BlendStateContext NormalisedTimeParam(ParameterData param)
    {
        base.NormalisedTimeParam(param);
        return this;
    }
#endif
    
    new public BlendStateContext Mirror(bool v)
    {
        base.Mirror(v);
        return this;
    }
    
    new public BlendStateContext MirrorParam(ParameterData param)
    {
        base.MirrorParam(param);
        return this;
    }
    
    new public BlendStateContext CycleOffset(float v)
    {
        base.CycleOffset(v);
        return this;
    }
    
    new public BlendStateContext CycleOffsetParam(ParameterData param)
    {
        base.CycleOffsetParam(param);
        return this;
    }
    
	new public BlendStateContext  Motion(Motion motion)
	{
        base.Motion(motion);
		return this;
	}
    
    new public BlendStateContext Tag(string tag)
    {
        base.Tag(tag);
        return this;
    }
    
	public BlendStateContext this[MotionContext motion]
	{
		get
		{
			data.motion = motion.data;
			return this;
		}
	}
}

public struct MotionContext
{
    public const int GT_Name = 1 << 0;
    public const int GT_ParamX = 1 << 1;
    public const int GT_ParamY = 1 << 2;
    public const int GT_ThresholdX = 1 << 3;
    public const int GT_ThresholdY = 1 << 4;
    public const int GT_Mirror = 1 << 5;
    public const int GT_Speed = 1 << 6;
    public const int GT_DirectParam = 1 << 7;
    public const int GT_AutomaticThresholds = 1 << 8;
    public const int GT_NormalisedBlendValues = 1 << 9;
    public const int GT_Clip = 1 << 10;
    
	public MotionData data;
    
	public MotionContext this[params MotionContext[] motions]
	{
		get
		{
			int count = motions.Length;
			for(int i = 0; i < count; ++i)
			{
				data.blendMotions.Add(motions[i].data);
			}
			return this;
		}
	}

	public MotionContext AddBlendMotions(params MotionContext[] motions)
	{
		int count = motions.Length;
		for(int i = 0; i < count; ++i)
		{
			data.blendMotions.Add(motions[i].data);
		}
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_Clip, false)]
    public MotionContext Clip(Motion clip)
    {
        data.clip = clip;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Name, false)]
    public MotionContext Name(string name)
    {
        data.name = name;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_ParamX, false)]
    public MotionContext ParamX(ParameterData param)
    {
        data.paramX = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_ParamY, false)]
    public MotionContext ParamY(ParameterData param)
    {
        data.paramY = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_ThresholdX, false)]
    public MotionContext ThresholdX(float v)
    {
        data.thresholdX = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_ThresholdY, false)]
    public MotionContext ThresholdY(float v)
    {
        data.thresholdY = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Mirror, false)]
    public MotionContext Mirror(bool mirror)
    {
        data.mirror = mirror;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Speed, false)]
    public MotionContext Speed(float speed)
    {
        data.speed = speed;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_DirectParam, false)]
    public MotionContext DirectParam(ParameterData param)
    {
        data.directParam = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_AutomaticThresholds, false)]
    public MotionContext AutomaticThresholds(bool automatic)
    {
        data.automaticThresholds = automatic;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_NormalisedBlendValues, false)]
    public MotionContext NormalisedBlendValues(bool normalisedBlendValues)
    {
        data.normalisedBlendValues = normalisedBlendValues;
        return this;
    }
}

public class StateMachineContext : StateMachineNodeContext
{
    public const int GT_Name = 1 << 0;
    public const int GT_DefaultState = 1 << 1;
    public const int GT_Tag = 1 << 2;
    
	public StateMachineContext this[params TOrTArray<StateMachineNodeContext>[] nodes]
	{
		get
		{
			AnimatorDataOps.AllocateAndMerge(nodes, (x) => x.data, data.nodes);
			return this;
		}
	}

	public StateMachineContext AddNodes(params TOrTArray<StateMachineNodeContext>[] nodes)
	{
		AnimatorDataOps.AllocateAndMerge(nodes, (x) => x.data, data.nodes);
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_Name, true)]
    public StateMachineContext Name(string name)
    {
        data.name = name;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_DefaultState, false)]
    public StateMachineContext DefaultState(string name)
    {
        data.defaultStateName = name;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Tag, false)]
    public StateMachineContext Tag(string tag)
    {
        data.tag = tag;
        return this;
    }
}

public struct LayerContext
{
    public const int GT_Weight = 1 << 0;
    public const int GT_AvatarMask = 1 << 1;
    public const int GT_SyncLayerName = 1 << 2;
    public const int GT_IKPass = 1 << 3;
    public const int GT_Timing = 1 << 4;
    public const int GT_BlendingMode = 1 << 5;
    public const int GT_DefaultState = 1 << 6;
    public const int GT_Tag = 1 << 7;
    public const int GT_Name = 1 << 8;
    
	public LayerData data;
    
	public LayerContext this[params TOrTArray<StateMachineNodeContext>[] nodes]
	{
		get
		{
			AnimatorDataOps.AllocateAndMerge(nodes, (x) => x.data, data.rootNode.nodes);
			return this;
		}
	}
    
    [AnimatorNodeInput(VCT.Single, GT_Weight, false)]
    public LayerContext Weight(float v)
    {
        data.weight = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_AvatarMask, false)]
    public LayerContext AvatarMask(AvatarMask mask)
    {
        data.mask = mask;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_SyncLayerName, false)]
    public LayerContext SyncLayerName(string v)
    {
        data.syncLayerName = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_IKPass, false)]
    public LayerContext IKPass(bool v)
    {
        data.ikPass = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Timing, false)]
    public LayerContext Timing(bool v)
    {
        data.timing = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_BlendingMode, false)]
    public LayerContext BlendingMode(LayerBlendingMode v)
    {
        data.blendingMode = v;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_DefaultState, false)]
    public LayerContext DefaultState(string name)
    {
        data.rootNode.defaultStateName = name;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Tag, false)]
    public LayerContext Tag(string tag)
    {
        data.rootNode.tag = tag;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Name, true)]
    public LayerContext Name(string name)
    {
        data.name = name;
        data.SetRootStateMachineName();
        return this;
    }
}

public struct TransitionGroupContext
{
	public AnimatorData data;
    
	public TransitionGroupContext this[params TOrTArray<TransitionContext>[] transitions]
	{
		get
		{
			AnimatorDataOps.AllocateAndMerge(transitions, (x) => x.data, data.transitions);
			return this;
		}
	}
}

public struct GraphContext
{
	public AnimatorData data;
    
	public GraphContext this[params TOrTArray<LayerContext>[] layers]
	{
		get
		{
			AnimatorDataOps.AllocateAndMerge(layers, (x) => x.data, data.layers);
			return this;
		}
	}

	public GraphContext AddLayers(params TOrTArray<LayerContext>[] layers)
	{
		AnimatorDataOps.AllocateAndMerge(layers, (x) => x.data, data.layers);
		return this;
	}
    
	public static implicit operator AnimatorData(GraphContext context)
	{
		return context.data;
	}
}

public struct TransitionContext
{
    public const int GT_ExitTime = 1 << 0;
    public const int GT_TransitionTime = 1 << 1;
    public const int GT_FixedTransitionTime = 1 << 2;
    public const int GT_TransitionOffset = 1 << 3;
    public const int GT_Interruption = 1 << 4;
    public const int GT_OrderedInterruption = 1 << 5;
    public const int GT_Destination = 1 << 6;
    public const int GT_Source = 1 << 7;
    
	public TransitionData data;
	public StateMachineNodeTable nodeTable;
    
	public TransitionContext this[params TOrTArray<TransitionConditionData>[] conditions]
	{
		get
		{
			SetConditions(conditions);
			return this;
		}
	}

	public TransitionContext AddConditions(params TOrTArray<TransitionConditionData>[] conditions)
	{
		AnimatorDataOps.AllocateAndMerge(conditions, (x) => x, data.conditions);
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_ExitTime, false)]
	public TransitionContext ExitTime(float value)
	{
		data.exitTime = value;
		return this;
	}

	public TransitionContext DefaultExitTime()
	{
		data.exitTime = TransitionData.UnityCalculatedExitTime;
		return this;
	}
    
	public TransitionContext TransitionTime(float value, bool isFixed)
	{
		data.transitionTime = value;
		data.isTransitionTimeFixed = isFixed;
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_TransitionTime, false)]
    public TransitionContext TransitionTime(float value)
    {
        data.transitionTime = value;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_FixedTransitionTime, false)]
    public TransitionContext FixedTransitionTime(bool isFixed)
    {
        data.isTransitionTimeFixed = isFixed;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, GT_TransitionOffset, false)]
	public TransitionContext TransitionOffset(float value)
	{
		data.transitionOffset = value;
		return this;
	}
    
	public TransitionContext Interruption(TransitionInterruptionType type, bool orderedInterruption)
	{
		data.interruptionType = type;
		data.orderedInterruption = orderedInterruption;
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_Interruption, false)]
    public TransitionContext Interruption(TransitionInterruptionType type)
	{
		data.interruptionType = type;
		return this;
	}
    
    [AnimatorNodeInput(VCT.Single, GT_OrderedInterruption, false)]
    public TransitionContext OrderedInterruption(bool orderedInterruption)
    {
        data.orderedInterruption = orderedInterruption;
        return this; 
    }
    
    [AnimatorNodeInput(VCT.Single, GT_Destination, true)]
	public TransitionContext Destination(AnimatorStateReference name)
	{
		data.destination = name;
		return this;
	}

    public TransitionContext Exit()
	{
        data.destination = AnimatorStateReference.ExitState();
		return this;
	}

    public TransitionContext Any()
	{
        AnimatorDataOps.AddSources(data, AnimatorStateReference.AnyState());
		return this;
	}
    
    [AnimatorNodeInput(VCT.Multiple, GT_Source, true)]
	public TransitionContext SourceRecursive(AnimatorStateReference name)
	{
		AnimatorDataOps.AddSourcesRecursive(data, name, nodeTable);
		return this;
	}
    
	public TransitionContext SourceMultiple(params AnimatorStateReference[] names)
	{
        AnimatorDataOps.AddSources(data, names);
		return this;
	}
    
    [AnimatorNodeInput(VCT.Multiple, GT_Source, true)]
	public TransitionContext Source(AnimatorStateReference name)
	{
		AnimatorDataOps.AddSources(data, name);
		return this;
	}
    
    public TransitionContext SetConditions(TOrTArray<TransitionConditionData>[] conditions)
    {
        AnimatorDataOps.AllocateAndMerge(conditions, (x) => x, data.conditions);
        return this;
    }
}

public struct TransitionConditionContext_GT
{
    public const int GT_Param = 1 << 0;
    public const int GT_Value = 1 << 1;
    public const int GT_Mode = 1 << 2;
}

public struct TransitionConditionContext_Bool
{
    public TransitionConditionData data;
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Param, true)]
    public TransitionConditionContext_Bool Param(ParameterData param)
    {
        data.parameter = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Value, true)]
    public TransitionConditionContext_Bool Value(bool value)
    {
        data.mode = value ? ConditionMode.If : ConditionMode.IfNot;
        return this;
    }
}
public struct TransitionConditionContext_Float
{
    public TransitionConditionData data;
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Param, true)]
    public TransitionConditionContext_Float Param(ParameterData param)
    {
        data.parameter = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Value, true)]
    public TransitionConditionContext_Float Value(float value)
    {
        data.threshold = value;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Mode, true)]
    public TransitionConditionContext_Float Mode(ConditionMode mode)
    {
        data.mode = mode;
        return this;
    }
}
public struct TransitionConditionContext_Int
{
    public TransitionConditionData data;
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Param, true)]
    public TransitionConditionContext_Int Param(ParameterData param)
    {
        data.parameter = param;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Value, true)]
    public TransitionConditionContext_Int Value(int value)
    {
        data.threshold = value;
        return this;
    }
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Mode, true)]
    public TransitionConditionContext_Int Mode(ConditionMode mode)
    {
        data.mode = mode;
        return this;
    }
}
public struct TransitionConditionContext_Trigger
{
    public TransitionConditionData data;
    
    [AnimatorNodeInput(VCT.Single, TransitionConditionContext_GT.GT_Param, true)]
    public TransitionConditionContext_Trigger Param(ParameterData param)
    {
        data.parameter = param;
        return this;
    }
}

// ==================================================================
// ====================== Animator Generation =======================
// ==================================================================
public class AnimatorGenerator
{
	public static Type animatorWindowType = null;
    
	static AnimatorGenerator()
	{
		// NOTE(broscoe): We need to cache the AnimatorWindowType because Unity marks it as internal so it's
		// not available to us at compile time. It's located in the UnityEditor.Graphs dll and is called "AnimatorControllerTool".
		// Setting the selectedGraphIndex will reinitialise all graph GUIs, even if the index was the same. Otherwise, when we
		// delete our layer and generate a new one, the window gets messed up and we need to select another layer before we can
		// see anything.
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		Assembly graphAssembly = null;
		for(int i = 0; i < assemblies.Length; ++i)
		{
			string name = assemblies[i].GetName().Name;
			if(name == "UnityEditor.Graphs")
			{
				graphAssembly = assemblies[i];
				break;
			}
		}
		
        Module graphModule = graphAssembly.GetModule("UnityEditor.Graphs.dll");
        Type[] types = graphModule.GetTypes();
		for(int i = 0; i < types.Length; ++i)
		{
			if(types[i].FullName == "UnityEditor.Graphs.AnimatorControllerTool")
			{
				animatorWindowType = types[i];
				break;
			}
		}
	}
    
	public struct DestinationStateOrMachine
	{
		public AnimatorStateWithParent state;
		public AnimatorStateMachineWithParent machine;
	}
	public static DestinationStateOrMachine LookupDestination(AnimatorStateTable stateTable, AnimatorMachineTable machineTable, int hash)
	{
		DestinationStateOrMachine result = new DestinationStateOrMachine();

		stateTable.TryGetValue(hash, out result.state);
		if(!result.state.IsValid())
		{
			machineTable.TryGetValue(hash, out result.machine);
		}

		return result;
	}

	public static void GenerateAnimator(AnimatorData data, AnimatorController controller)
	{
		AnimatorPositionTable positionTable = new AnimatorPositionTable();
		
		// NOTE(broscoe): Clean up old animator. Delete everything and regenerate.
        // NOTE(broscoe): This was initially looping through from length -> 0. That causes Unity to throw an out of range exception
        // for whatever reason.
        RefreshUnityAnimatorWindow();
		LoadAnimatorPositionData(controller, positionTable);

		bool foundScriptableObject = false;
		UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller));
		for(int i = 0; i < subAssets.Length; ++i)
		{
			if(subAssets[i] != null)
			{
				if(subAssets[i] != controller)
				{
					if(subAssets[i] is CSharpAnimator)
					{
						foundScriptableObject = true;
					}
					else
					{
						Assert.IsTrue(subAssets[i] is BlendTree 
                                      || subAssets[i] is AnimatorStateMachine
                                      || subAssets[i] is AnimatorState
                                      || subAssets[i] is AnimatorStateTransition, 
                                      string.Format("Found sub asset of type {0}", subAssets[i].GetType().ToString()));

						// NOTE(broscoe): Prevents null reference exception when the user has a sub asset selected.
						UnityEngine.Object.DestroyImmediate(subAssets[i], true);
					}
				}
			}
		}
		Assert.IsTrue(foundScriptableObject, "Could not find scriptable object");

		for(int i = 0; i < controller.layers.Length; ++i)
		{
			controller.RemoveLayer(i);
            --i;
		}
		for(int i = controller.parameters.Length - 1; i >= 0; --i)
		{
			controller.RemoveParameter(i);
		}
		
		// NOTE(broscoe): Keep a table of name hash -> animator state for quickly looking up states from transition names.
		AnimatorStateTable stateTable = new AnimatorStateTable();
		AnimatorMachineTable machineTable = new AnimatorMachineTable();
        
		for(int i = 0; i < data.layers.Count; ++i)
		{
			LayerData layerData = data.layers[i];
			AnimatorControllerLayer layer = new AnimatorControllerLayer();
            
			layer.name = layerData.name;
            layer.avatarMask = layerData.mask;
            layer.defaultWeight = layerData.weight;
            layer.iKPass = layerData.ikPass;
            layer.blendingMode = layerData.blendingMode == LayerBlendingMode.Override ? AnimatorLayerBlendingMode.Override : AnimatorLayerBlendingMode.Additive;
            
            int layerIndex = AnimatorDataOps.GetLayerIndexFromName(data, layerData.syncLayerName);
            layer.syncedLayerAffectsTiming = layerData.timing;
            layer.syncedLayerIndex = layerIndex;
            
			GenerateNodeRecursive(controller, layerData.rootNode, null, null, layer, stateTable, machineTable, positionTable);
			controller.AddLayer(layer);
		}
        
		for(int i = 0; i < data.parameters.Count; ++i)
		{
			ParameterData paramData = data.parameters[i];
			AnimatorControllerParameter parameter = new AnimatorControllerParameter();
            
			parameter.name = paramData.name;
			switch(paramData.type)
			{
				case ParameterType.Bool:
				{
					parameter.type = AnimatorControllerParameterType.Bool;
					parameter.defaultBool = paramData.defaultBoolValue;
				}
				break;
                
				case ParameterType.Float:
				{
					parameter.type = AnimatorControllerParameterType.Float;
					parameter.defaultFloat = paramData.defaultFloatValue;
				}
				break;
                
				case ParameterType.Int:
				{
					parameter.type = AnimatorControllerParameterType.Int;
					parameter.defaultInt = paramData.defaultIntValue;
				}
				break;
                
				case ParameterType.Trigger:
				{
					parameter.type = AnimatorControllerParameterType.Trigger;
				}
				break;
			}
            
			controller.AddParameter(parameter);
		}
        
		if(data.transitions != null)
		{
			for(int i = 0; i < data.transitions.Count; ++i)
			{
				TransitionData transitionData = data.transitions[i];

                bool isExit = transitionData.destination.type == AnimatorStateReferenceType.Exit;
                int destinationHash = 0;
                DestinationStateOrMachine destination = new DestinationStateOrMachine();

                if(!isExit)
                {
			        destinationHash = Animator.StringToHash(transitionData.destination.name);
			        destination = LookupDestination(stateTable, machineTable, destinationHash);
			        if(!destination.state.IsValid() && !destination.machine.IsValid())
			        {
				        Debug.LogWarning(string.Format("Transition with name '{0}' has destination state or state machine '{1}', which does not exist.", transitionData.name, transitionData.destination));
				        continue;
			        }
                }
            
				for(int s = 0; s < transitionData.sourceCount; ++s)
				{
					AnimatorStateReference source = transitionData.sources[s];
					int sourceHash = Animator.StringToHash(source.name);
                
					AnimatorStateWithParent sourceState;
					stateTable.TryGetValue(sourceHash, out sourceState);
                
					if(source.type == AnimatorStateReferenceType.Normal && !sourceState.IsValid())
					{
						Debug.LogWarning(string.Format("Transition with name '{0}' has source state '{1}' which does not exist.", transitionData.name, source));
						continue;
					}
                    if(source.type == AnimatorStateReferenceType.Exit)
                    {
                        Debug.LogWarning(string.Format("Transition with name '{0}' has Exit source state which is not allowed.", transitionData.name));
                        continue;
                    }
                
					bool unityCalculatedExitTime = transitionData.exitTime <= TransitionData.UnityCalculatedExitTime;
                    
					AnimatorStateTransition transition = null;
					if(destination.state.IsValid() || isExit)
					{
                        if(!isExit)
                        {
                            switch(source.type)
                            {
                                case AnimatorStateReferenceType.Normal:
                                {
                                    transition = sourceState.state.AddTransition(destination.state.state, unityCalculatedExitTime);
                                }
                                break;

                                case AnimatorStateReferenceType.Any:
                                {
                                    Assert.IsTrue(destination.state.parent != null);
                                    transition = destination.state.parent.AddAnyStateTransition(destination.state.state);
                                }
                                break;
                            }
                        }
                        else
                        {
                            if(source.type == AnimatorStateReferenceType.Any)
                            {
                                Debug.LogWarning(string.Format("Transition with name '{0}' has Exit desination with Any as a source. This is not allowed.", transitionData.name));
                                continue;
                            }
                            transition = sourceState.state.AddExitTransition(unityCalculatedExitTime);
                        }
					}
					else
					{
                        switch(source.type)
                        {
                            case AnimatorStateReferenceType.Normal:
                            {
                                transition = sourceState.state.AddTransition(destination.machine.machine, unityCalculatedExitTime);
                            }
                            break;

                            case AnimatorStateReferenceType.Any:
                            {
                                Assert.IsTrue(destination.machine.parent != null);
                                transition = destination.machine.parent.AddAnyStateTransition(destination.machine.machine);
                            }
                            break;
                        }
					}
					InitialiseAnimatorTransition(transition, transitionData, unityCalculatedExitTime);
				}
			}
		}
        
		RefreshUnityAnimatorWindow();
		AssetDatabase.SaveAssets();
	}
    
    public static void InitialiseAnimatorTransition(AnimatorStateTransition transition, TransitionData transitionData, bool unityCalculatedExitTime)
    {
        transition.name = transitionData.name;
        transition.hasExitTime = transitionData.exitTime > TransitionData.NoExitTime || unityCalculatedExitTime;

		if(!unityCalculatedExitTime)
		{ 
			transition.exitTime = transitionData.exitTime;
		}
		
        if(transitionData.transitionTime > TransitionData.TransitionTimeDefault)
        {
            transition.duration = transitionData.transitionTime;
        }
        transition.hasFixedDuration = transitionData.isTransitionTimeFixed;
        transition.offset = transitionData.transitionOffset;
        
        switch(transitionData.interruptionType)
        {
            case TransitionInterruptionType.None: transition.interruptionSource = TransitionInterruptionSource.None; break;
            case TransitionInterruptionType.Source: transition.interruptionSource = TransitionInterruptionSource.Source; break;
            case TransitionInterruptionType.SourceThenDestination: transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination; break;
            case TransitionInterruptionType.DestinationThenSource: transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource; break;
            case TransitionInterruptionType.Destination: transition.interruptionSource = TransitionInterruptionSource.Destination; break;
        }
        transition.orderedInterruption = transitionData.orderedInterruption;
        
        if(transitionData.conditions != null)
        {
            for(int c = 0; c < transitionData.conditions.Count; ++c)
            {
                TransitionConditionData conditionData = transitionData.conditions[c];
                
                AnimatorConditionMode mode = AnimatorConditionMode.Equals;
                switch(conditionData.mode)
                {
                    case ConditionMode.Equals: mode = AnimatorConditionMode.Equals; break;
                    case ConditionMode.Greater: mode = AnimatorConditionMode.Greater; break;
                    case ConditionMode.If: mode = AnimatorConditionMode.If; break;
                    case ConditionMode.IfNot: mode = AnimatorConditionMode.IfNot; break;
                    case ConditionMode.Less: mode = AnimatorConditionMode.Less; break;
                    case ConditionMode.NotEqual: mode = AnimatorConditionMode.NotEqual; break;
                }
                transition.AddCondition(mode, conditionData.threshold, conditionData.parameter.name);
            }
        }
    }

	public static void GenerateNodeRecursive(AnimatorController controller, StateMachineNodeData node, string outterTag,
											 AnimatorStateMachine parentMachine, AnimatorControllerLayer parentLayer, 
											 AnimatorStateTable stateTable, AnimatorMachineTable machineTable, AnimatorPositionTable positionTable)
	{
		int nameHash = Animator.StringToHash(node.name);
		
		AnimatorNodePosition position;
		bool foundPosition = positionTable.TryGetValue(nameHash, out position);
        
        string thisTag = node.tag;
        if(thisTag == null)
        {
            thisTag = outterTag;
        }

		if(node.type == StateMachineNodeType.State)
		{
			Assert.IsTrue(parentMachine != null && parentLayer == null);

			AnimatorState state = new AnimatorState();
			state.hideFlags |= HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(state, controller);

			if(node.motion == null || node.motion.type == MotionDataType.Clip)
			{
				state.motion = node.motion == null ? null : node.motion.clip;
			}
			else
			{
				BlendTree root = new BlendTree();
				root.hideFlags |= HideFlags.HideInHierarchy;

				// NOTE(broscoe): Taken from AnimatorController.cs, since we don't want this blend state to
				// end up in layer.statemachine.
				AssetDatabase.AddObjectToAsset(root, controller);

				GenerateBlendTreeRecursive(root, node.motion);
				state.motion = root;
			}
            
            state.tag = thisTag;
			state.name = node.name;
            state.iKOnFeet = node.ikOnFeet;
            state.writeDefaultValues = node.writeDefaultValues;
            
            state.speed = node.speed;
            if(node.speedParam != null)
            {
                state.speedParameterActive = true;
                state.speedParameter = node.speedParam.name;
            }
            
			/*
            if(node.normalisedTimeParam != null)
            {
                state.timeParameterActive = true;
                state.timeParameter = node.normalisedTimeParam.name;
            }
            */

            state.mirror = node.mirror;
            if(node.mirrorParam != null)
            {
                state.mirrorParameterActive = true;
                state.mirrorParameter = node.mirrorParam.name;
            }
            
            state.cycleOffset = node.cycleOffset;
            if(node.cycleOffsetParam != null)
            {
                state.cycleOffsetParameterActive = true;
                state.cycleOffsetParameter = node.cycleOffsetParam.name;
            }
			
			parentMachine.AddState(state, position.position);
			
			if(!stateTable.ContainsKey(nameHash))
			{ 
                AnimatorStateWithParent withParent;
                withParent.state = state;
                withParent.parent = parentMachine;
				stateTable.Add(nameHash, withParent);
			}
		}
		else
		{
			Assert.IsTrue(parentMachine != null || parentLayer != null);
			Assert.IsTrue(parentMachine == null || parentLayer == null);
			bool isParentLayer = parentLayer == null ? false : true;

			AnimatorStateMachine machine = new AnimatorStateMachine();
			machine.hideFlags |= HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(machine, controller);
			machine.name = node.name;

			if(node.nodes != null)
			{
				for(int i = 0; i < node.nodes.Count; ++i)
				{
					GenerateNodeRecursive(controller, node.nodes[i], thisTag, machine, null, stateTable, machineTable, positionTable);
				}
			}

			if(isParentLayer)
			{
				parentLayer.stateMachine = machine;
			}
			else
			{
				parentMachine.AddStateMachine(machine, position.position);
			}
            
			// NOTE(broscoe): Unity provides some reasonable default position if we didn't find one.
			if(foundPosition)
			{
				machine.parentStateMachinePosition = position.position;
				machine.anyStatePosition = position.anyPosition;
				machine.entryPosition = position.entryPosition;
				machine.exitPosition = position.exitPosition;
			}
            
            string defaultStateName = node.defaultStateName;
            if(defaultStateName != null)
            {
                AnimatorStateWithParent defaultState = new AnimatorStateWithParent();
                int defaultHash = Animator.StringToHash(defaultStateName);
                stateTable.TryGetValue(defaultHash, out defaultState);
                
                Assert.IsTrue(defaultState.IsValid(), string.Format("Could not find default state {0}", defaultStateName));
                if(defaultState.IsValid())
                {
                    machine.defaultState = defaultState.state;
                }
            }

			if(machine != null)
			{
                AnimatorStateMachineWithParent withParent = new AnimatorStateMachineWithParent();
                withParent.machine = machine;
                withParent.parent = parentMachine;
				machineTable.Add(nameHash, withParent);
			}
		}
	}

	public static BlendTree GenerateBlendTreeRecursive(BlendTree tree, MotionData data)
	{
		tree.name = data.name;
//        tree.normalisedBlendValues = data.normalisedBlendValues;
		if(data.paramX != null)
		{
			tree.blendParameter = data.paramX.name;
		}
		if(data.paramY != null)
		{
			tree.blendParameterY = data.paramY.name;
		}
        
        switch(data.blendTreeType)
        {
            case TreeType.BlendTree1D: tree.blendType = BlendTreeType.Simple1D; break;
            case TreeType.BlendTree2D_SimpleDirectional: tree.blendType = BlendTreeType.SimpleDirectional2D; break;
            case TreeType.BlendTree2D_FreeFormDirectional: tree.blendType = BlendTreeType.FreeformDirectional2D; break;
            case TreeType.BlendTree2D_FreeFormCartesian: tree.blendType = BlendTreeType.FreeformCartesian2D; break;
            case TreeType.BlendTreeDirect: tree.blendType = BlendTreeType.Direct; break;
            default: Assert.IsTrue(false, "Unknown BlendTreeType while generating."); break;
        }
        tree.useAutomaticThresholds = data.automaticThresholds;

        if(data.blendMotions != null)
        {
            for(int i = 0; i < data.blendMotions.Count; ++i)
            {
                MotionData childData = data.blendMotions[i];
                if(childData.type == MotionDataType.BlendTree)
                {
                    BlendTree child = null;
                    child = tree.CreateBlendTreeChild(childData.thresholdX);
                    GenerateBlendTreeRecursive(child, childData);
                }
                else
                {
                    tree.AddChild(data.blendMotions[i].clip);
                    
                    // TODO(brosoce): Is there a better way to do this? This makes a copy of the array every time.
                    ChildMotion[] children = tree.children;
                    int index = tree.children.Length - 1;
                    ChildMotion child = children[index];
                    child.position = new Vector2(childData.thresholdX, childData.thresholdY);
                    child.threshold = childData.thresholdX;
                    child.mirror = childData.mirror;
                    child.timeScale = childData.speed;
                    
                    if(childData.directParam != null)
                    {
                        child.directBlendParameter = childData.directParam.name;
                    }
                    
                    children[index] = child;
                    tree.children = children;
                }
            }
        }
		return tree;
	}

	public static void LoadAnimatorPositionData(AnimatorController controller, AnimatorPositionTable table)
	{
		for(int i = 0; i < controller.layers.Length; ++i)
		{
			if(controller.layers[i] != null && controller.layers[i].stateMachine != null)
			{
				LoadStateMachinePositionDataRecursive(controller.layers[i].stateMachine, Vector2.zero, table);
			}
		}
	}

	public static void LoadStateMachinePositionDataRecursive(AnimatorStateMachine stateMachine, Vector2 stateMachinePositionInParent, 
															 AnimatorPositionTable table)
	{
		AnimatorNodePosition position = new AnimatorNodePosition();
		position.position = stateMachinePositionInParent;
		position.parentPosition = stateMachine.parentStateMachinePosition;
		position.anyPosition = stateMachine.anyStatePosition;
		position.entryPosition = stateMachine.entryPosition;
		position.exitPosition = stateMachine.exitPosition;

		int nameHash = Animator.StringToHash(stateMachine.name);
        if(table.ContainsKey(nameHash))
        {
            Debug.LogWarning(string.Format("{0} found name conflict '{1}'", "LoadStateMachinePositionDataRecursive", stateMachine.name));
        }
        else
        {
            table.Add(nameHash, position);
        }

		for(int i = 0; i < stateMachine.states.Length; ++i)
		{
			ChildAnimatorState child = stateMachine.states[i];

			AnimatorNodePosition childPosition = new AnimatorNodePosition();
			childPosition.position = child.position;

			int childNameHash = child.state.nameHash;
			if(table.ContainsKey(childNameHash))
			{
				Debug.LogWarning(string.Format("{0} found name conflict '{1}'", "LoadStateMachinePositionDataRecursive", child.state.name));
			}
			else
			{ 
				table.Add(childNameHash, childPosition);
			}
		}
		for(int i = 0; i < stateMachine.stateMachines.Length; ++i)
		{
			ChildAnimatorStateMachine child = stateMachine.stateMachines[i];
			LoadStateMachinePositionDataRecursive(child.stateMachine, child.position, table);
		}
	}

    public static EditorWindow animatorWindowReference = null;
	public static void RefreshUnityAnimatorWindow()
	{
		if(animatorWindowType != null)
		{
            if(animatorWindowReference == null)
            {
                animatorWindowReference = EditorWindow.GetWindow(animatorWindowType, false, null, false);
            }
			
            if(animatorWindowReference)
            {
                PropertyInfo selectedLayerProperty = animatorWindowType.GetProperty("selectedLayerIndex", BindingFlags.Public | BindingFlags.Instance);
                selectedLayerProperty.SetValue(animatorWindowReference, 0, null);
            }
		}
	}
}


// ==================================================================
// ============================= Editor =============================
// ==================================================================

[CustomEditor(typeof(AnimatorController))]
public class TA_AnimatorControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		AnimatorController targetController = target as AnimatorController;
		if(targetController != null)
		{
			string thisPath = AssetDatabase.GetAssetPath(target);

			CSharpAnimator textAnimatorObject = null;
			UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(thisPath);
			for(int i = 0; i < subAssets.Length; ++i)
			{
				if(subAssets[i] != null)
				{
					Type type = subAssets[i].GetType();
					if(type == typeof(CSharpAnimator) || type.IsSubclassOf(typeof(CSharpAnimator)))
					{
						textAnimatorObject = (CSharpAnimator)subAssets[i];
						break;
					}
				}
			}
		
			if(textAnimatorObject != null)
			{
				Editor editor = Editor.CreateEditor(textAnimatorObject, typeof(TextAnimatorEditor));
				if(editor != null && editor.target != null)
				{
					editor.OnInspectorGUI();

					if(textAnimatorObject.IsValidForGeneration())
					{
						if(GUILayout.Button("Generate"))
						{
							textAnimatorObject.GenerateInto(targetController);
						}
					}
				}
			}
		}
	}
}

[CustomEditor(typeof(CSharpAnimator), true)]
public class TextAnimatorEditor : Editor
{
    [NonSerialized]
    public Vector2 genericScrollPosition;
    
    public void DrawBaseInspector()
    {
        base.OnInspectorGUI();
    }

	public override void OnInspectorGUI()
	{
		CSharpAnimator animator = serializedObject.targetObject as CSharpAnimator;
		if(animator != null)
		{
			animator.Module_DrawInspector(this, serializedObject);
            
			serializedObject.ApplyModifiedProperties();
			Repaint();
		}
	}
}
#endif