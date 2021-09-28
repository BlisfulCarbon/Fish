#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Assertions;

[System.Serializable]
public class AnimatorSceneTestEntry
{
	[SerializeField]
	public AnimatorController controller;
}

public class AnimatorSceneTest : MonoBehaviour
{
	[SerializeField]
	public List<AnimatorSceneTestEntry> entries = new List<AnimatorSceneTestEntry>();
	[SerializeField]
	public Animator targetAnimatorPrefab = null;

	[NonSerialized]
	public int entryIndex = 0;
	[NonSerialized]
	public Animator targetAnimator = null;

	public void Awake()
	{
		Assert.IsTrue(targetAnimatorPrefab != null, "AnimatorSceneTest targetAnimatorPrefab cannot be null.");

		GameObject newObj = Instantiate(targetAnimatorPrefab.gameObject);
		targetAnimator = newObj.GetComponent<Animator>();
		targetAnimator.applyRootMotion = false;

		SetEntryIndex(0);
	}

	public void Update()
	{
		int lastIndex = entryIndex;
		int newIndex = lastIndex;
		if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			newIndex = entryIndex - 1;
		}
		if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			newIndex = entryIndex + 1;
		}

		if(lastIndex != newIndex)
		{
			if(newIndex >= entries.Count)
			{
				newIndex = 0;
			}
			else if(newIndex < 0)
			{
				newIndex = entries.Count - 1;
			}

			SetEntryIndex(newIndex);
		}
	}

	public void OnGUI()
	{
		AnimatorSceneTestEntry entry = (entryIndex < entries.Count && entryIndex >= 0) ? entries[entryIndex] : null;

		if(entry != null)
		{
			const float HorizontalSpacing = 50.0f;
			GUILayoutOption width = GUILayout.Width(250.0f);

			GUILayout.Space(25.0f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(HorizontalSpacing);
			GUIStyle titleStyle = new GUIStyle();
			titleStyle.fontSize = 32;
			GUILayout.Label(entry.controller.name, titleStyle, GUILayout.Width(400.0f), GUILayout.Width(250.0f));
			GUILayout.EndHorizontal();

			GUILayout.Space(15.0f);
			AnimatorControllerParameter[] parameters = entry.controller.parameters;
			for(int i = 0; i < entry.controller.parameters.Length; ++i)
			{
				AnimatorControllerParameter param = parameters[i];
				
				GUILayout.BeginHorizontal();
				GUILayout.Space(HorizontalSpacing);
				switch(param.type)
				{
					case AnimatorControllerParameterType.Bool:
					{
						bool value = targetAnimator.GetBool(param.nameHash);
						value = GUILayout.Toggle(value, param.name, width);
						targetAnimator.SetBool(param.nameHash, value);
					}
					break;

					case AnimatorControllerParameterType.Int:
					{
						int value = targetAnimator.GetInteger(param.nameHash);
						GUILayout.Label(param.name + " ", width);
						value = (int)GUILayout.HorizontalSlider(value, 0, 10, width);
						targetAnimator.SetInteger(param.nameHash, value);
					}
					break;

					case AnimatorControllerParameterType.Float:
					{
						float value = targetAnimator.GetFloat(param.nameHash);
						GUILayout.Label(param.name + " ");
						value = GUILayout.HorizontalSlider(value, 0, 10, width);
						targetAnimator.SetFloat(param.nameHash, value);
					}
					break;

					case AnimatorControllerParameterType.Trigger:
					{
						if(GUILayout.Button(param.name, width))
						{
							targetAnimator.SetTrigger(param.nameHash);
						}
					}
					break;
				}
				GUILayout.EndHorizontal();
			}
		}
	}

	public void SetEntryIndex(int index)
	{
		entryIndex = index;
		AnimatorSceneTestEntry entry = entries[entryIndex];
		targetAnimator.runtimeAnimatorController = entry.controller;
	}
}
#endif