using HarmonyLib;
using BepInEx;
using System.Reflection;

namespace DequeCraft
{
	[BepInPlugin("org.bepinex.plugins.dequecraft", "Deque", "1.0.1")]
	public class DequeCraft : BaseUnityPlugin
	{
		internal void Awake()
		{
			var harmony = new Harmony("org.bepinex.plugins.dequecraft");
			Harmony.CreateAndPatchAll(typeof(DequeCraft));
		}

		private static bool tempflag;
		private static int beforeLen;
		private static bool rightClick;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIReplicatorWindow), "_OnCreate")]
		private static void AfterOnCreate(UIReplicatorWindow __instance, UIButton ___okButton)
		{
			___okButton.onRightClick += (whatever) => RightClick(__instance);
		}

		private static void RightClick(UIReplicatorWindow replicator)
		{
			rightClick = true;
			MethodInfo buttonClick = replicator.GetType().GetMethod("OnOkButtonClick", BindingFlags.NonPublic | BindingFlags.Instance);
			buttonClick.Invoke(replicator, new object[] { 0, true });
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MechaForge), "AddTask")]
		private static bool BeforeAddTask(MechaForge __instance, int recipeId, int count) 
		{
			if (!__instance.gameHistory.RecipeUnlocked(recipeId))
			{
				rightClick = false;
				return false;
			}
			tempflag = __instance.TryAddTask(recipeId, count);
			if (tempflag)
			{
				beforeLen = __instance.tasks.Count;
				return true;
			}
			rightClick = false;
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MechaForge), "AddTask")]
		private static void AfterAddTask(MechaForge __instance, int recipeId, int count)
		{
			if (tempflag)
			{
				if (rightClick)
				{
					rightClick = false;
					return;
				}

				if (beforeLen == 0)
					return;

				int curLen = __instance.tasks.Count;
				int gap = curLen - beforeLen;

				if (gap == 0)
					return;

				for (int i = 0; i < beforeLen; i++)
				{
					if (__instance.tasks[i].parentTaskIndex != -1)
					{
						__instance.tasks[i].parentTaskIndex += gap;
					}
				}

				for (int i = beforeLen; i < curLen; i++)
				{
					ForgeTask t = __instance.tasks[curLen - 1];
					if (t.parentTaskIndex != -1)
					{
						t.parentTaskIndex -= beforeLen;
					}
					__instance.tasks.RemoveAt(curLen - 1);
					__instance.tasks.Insert(0, t);
				}
			}
		}
	}
}