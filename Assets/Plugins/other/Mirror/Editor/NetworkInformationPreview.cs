using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mirror
{
    [CustomPreview(typeof(GameObject))]
    internal class NetworkInformationPreview : ObjectPreview
    {
        private Styles styles = new();

        private GUIContent title;

        public override GUIContent GetPreviewTitle()
        {
            if (title == null) title = new GUIContent("Network Information");
            return title;
        }

        public override bool HasPreviewGUI()
        {
            // need to check if target is null to stop MissingReferenceException
            return target != null && target is GameObject gameObject &&
                   gameObject.GetComponent<NetworkIdentity>() != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (target == null)
                return;

            var targetGameObject = target as GameObject;

            if (targetGameObject == null)
                return;

            var identity = targetGameObject.GetComponent<NetworkIdentity>();

            if (identity == null)
                return;

            if (styles == null)
                styles = new Styles();


            // padding
            var previewPadding = new RectOffset(-5, -5, -5, -5);
            var paddedr = previewPadding.Add(r);

            //Centering
            var initialX = paddedr.x + 10;
            var Y = paddedr.y + 10;

            Y = DrawNetworkIdentityInfo(identity, initialX, Y);

            Y = DrawNetworkBehaviors(identity, initialX, Y);

            Y = DrawObservers(identity, initialX, Y);

            _ = DrawOwner(identity, initialX, Y);
        }

        private float DrawNetworkIdentityInfo(NetworkIdentity identity, float initialX, float Y)
        {
            var infos = GetNetworkIdentityInfo(identity);
            // Get required label size for the names of the information values we're going to show
            // There are two columns, one with label for the name of the info and the next for the value
            var maxNameLabelSize = new Vector2(140, 16);
            var maxValueLabelSize = GetMaxNameLabelSize(infos);

            var labelRect = new Rect(initialX, Y, maxNameLabelSize.x, maxNameLabelSize.y);
            var idLabelRect = new Rect(maxNameLabelSize.x, Y, maxValueLabelSize.x, maxValueLabelSize.y);

            foreach (var info in infos)
            {
                GUI.Label(labelRect, info.name, styles.labelStyle);
                GUI.Label(idLabelRect, info.value, styles.componentName);
                labelRect.y += labelRect.height;
                labelRect.x = initialX;
                idLabelRect.y += idLabelRect.height;
            }

            return labelRect.y;
        }

        private float DrawNetworkBehaviors(NetworkIdentity identity, float initialX, float Y)
        {
            var behavioursInfo = GetNetworkBehaviorInfo(identity);

            // Show behaviours list in a different way than the name/value pairs above
            var maxBehaviourLabelSize = GetMaxBehaviourLabelSize(behavioursInfo);
            var behaviourRect = new Rect(initialX, Y + 10, maxBehaviourLabelSize.x, maxBehaviourLabelSize.y);

            GUI.Label(behaviourRect, new GUIContent("Network Behaviours"), styles.labelStyle);
            // indent names
            behaviourRect.x += 20;
            behaviourRect.y += behaviourRect.height;

            foreach (var info in behavioursInfo)
            {
                if (info.behaviour == null)
                    // could be the case in the editor after existing play mode.
                    continue;

                GUI.Label(behaviourRect, info.name,
                    info.behaviour.enabled ? styles.componentName : styles.disabledName);
                behaviourRect.y += behaviourRect.height;
                Y = behaviourRect.y;
            }

            return Y;
        }

        private float DrawObservers(NetworkIdentity identity, float initialX, float Y)
        {
            if (identity.observers.Count > 0)
            {
                var observerRect = new Rect(initialX, Y + 10, 200, 20);

                GUI.Label(observerRect, new GUIContent("Network observers"), styles.labelStyle);
                // indent names
                observerRect.x += 20;
                observerRect.y += observerRect.height;

                foreach (var kvp in identity.observers)
                {
                    GUI.Label(observerRect, $"{kvp.Value.address}:{kvp.Value}", styles.componentName);
                    observerRect.y += observerRect.height;
                    Y = observerRect.y;
                }
            }

            return Y;
        }

        private float DrawOwner(NetworkIdentity identity, float initialX, float Y)
        {
            if (identity.connectionToClient != null)
            {
                var ownerRect = new Rect(initialX, Y + 10, 400, 20);
                GUI.Label(ownerRect, new GUIContent($"Client Authority: {identity.connectionToClient}"),
                    styles.labelStyle);
                Y += ownerRect.height;
            }

            return Y;
        }

        // Get the maximum size used by the value of information items
        private Vector2 GetMaxNameLabelSize(IEnumerable<NetworkIdentityInfo> infos)
        {
            var maxLabelSize = Vector2.zero;
            foreach (var info in infos)
            {
                var labelSize = styles.labelStyle.CalcSize(info.value);
                if (maxLabelSize.x < labelSize.x) maxLabelSize.x = labelSize.x;
                if (maxLabelSize.y < labelSize.y) maxLabelSize.y = labelSize.y;
            }

            return maxLabelSize;
        }

        private Vector2 GetMaxBehaviourLabelSize(IEnumerable<NetworkBehaviourInfo> behavioursInfo)
        {
            var maxLabelSize = Vector2.zero;
            foreach (var behaviour in behavioursInfo)
            {
                var labelSize = styles.labelStyle.CalcSize(behaviour.name);
                if (maxLabelSize.x < labelSize.x) maxLabelSize.x = labelSize.x;
                if (maxLabelSize.y < labelSize.y) maxLabelSize.y = labelSize.y;
            }

            return maxLabelSize;
        }

        private IEnumerable<NetworkIdentityInfo> GetNetworkIdentityInfo(NetworkIdentity identity)
        {
            var infos = new List<NetworkIdentityInfo>
            {
                GetAssetId(identity),
                GetString("Scene ID", identity.sceneId.ToString("X"))
            };

            if (Application.isPlaying)
            {
                infos.Add(GetString("Network ID", identity.netId.ToString()));
                infos.Add(GetBoolean("Is Client", identity.isClient));
                infos.Add(GetBoolean("Is Server", identity.isServer));
                infos.Add(GetBoolean("Is Owned", identity.isOwned));
                infos.Add(GetBoolean("Is Local Player", identity.isLocalPlayer));
            }

            return infos;
        }

        private IEnumerable<NetworkBehaviourInfo> GetNetworkBehaviorInfo(NetworkIdentity identity)
        {
            var behaviourInfos = new List<NetworkBehaviourInfo>();

            var behaviours = identity.GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
                behaviourInfos.Add(new NetworkBehaviourInfo
                {
                    name = new GUIContent(behaviour.GetType().FullName),
                    behaviour = behaviour
                });
            return behaviourInfos;
        }

        private NetworkIdentityInfo GetAssetId(NetworkIdentity identity)
        {
            var assetId = identity.assetId.ToString();
            if (string.IsNullOrWhiteSpace(assetId)) assetId = "<object has no prefab>";
            return GetString("Asset ID", assetId);
        }

        private static NetworkIdentityInfo GetString(string name, string value)
        {
            return new NetworkIdentityInfo
            {
                name = new GUIContent(name),
                value = new GUIContent(value)
            };
        }

        private static NetworkIdentityInfo GetBoolean(string name, bool value)
        {
            return new NetworkIdentityInfo
            {
                name = new GUIContent(name),
                value = new GUIContent(value ? "Yes" : "No")
            };
        }

        private struct NetworkIdentityInfo
        {
            public GUIContent name;
            public GUIContent value;
        }

        private struct NetworkBehaviourInfo
        {
            // This is here just so we can check if it's enabled/disabled
            public NetworkBehaviour behaviour;
            public GUIContent name;
        }

        private class Styles
        {
            public readonly GUIStyle componentName = new(EditorStyles.boldLabel);
            public readonly GUIStyle disabledName = new(EditorStyles.miniLabel);
            public readonly GUIStyle labelStyle = new(EditorStyles.label);

            public Styles()
            {
                var fontColor = new Color(0.7f, 0.7f, 0.7f);
                labelStyle.padding.right += 20;
                labelStyle.normal.textColor = fontColor;
                labelStyle.active.textColor = fontColor;
                labelStyle.focused.textColor = fontColor;
                labelStyle.hover.textColor = fontColor;
                labelStyle.onNormal.textColor = fontColor;
                labelStyle.onActive.textColor = fontColor;
                labelStyle.onFocused.textColor = fontColor;
                labelStyle.onHover.textColor = fontColor;

                componentName.normal.textColor = fontColor;
                componentName.active.textColor = fontColor;
                componentName.focused.textColor = fontColor;
                componentName.hover.textColor = fontColor;
                componentName.onNormal.textColor = fontColor;
                componentName.onActive.textColor = fontColor;
                componentName.onFocused.textColor = fontColor;
                componentName.onHover.textColor = fontColor;

                disabledName.normal.textColor = fontColor;
                disabledName.active.textColor = fontColor;
                disabledName.focused.textColor = fontColor;
                disabledName.hover.textColor = fontColor;
                disabledName.onNormal.textColor = fontColor;
                disabledName.onActive.textColor = fontColor;
                disabledName.onFocused.textColor = fontColor;
                disabledName.onHover.textColor = fontColor;
            }
        }
    }
}