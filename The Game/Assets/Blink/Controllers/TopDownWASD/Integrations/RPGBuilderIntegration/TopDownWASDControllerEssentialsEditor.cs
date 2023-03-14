using BLINK.RPGBuilder.Controller;
using UnityEditor;

#if UNITY_EDITOR
namespace BLINK.Controller
{
    [CustomEditor(typeof(TopDownWASDControllerEssentials))]
    public class TopDownWASDControllerEssentialsEditor : Editor
    {

        public override void OnInspectorGUI()
        {

        }
    }
}
#endif
