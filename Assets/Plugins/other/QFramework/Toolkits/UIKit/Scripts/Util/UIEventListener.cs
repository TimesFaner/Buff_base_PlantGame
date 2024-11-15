﻿/****************************************************************************
 * Copyright (c) 2017 liangxie
 ****************************************************************************/

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QFramework
{
    public class UIEventListener : EventTrigger
    {
        public Action<BaseEventData> onBeginDrag;

        // TODO: refactor proper name
        public Action onClick;
        public Action<BaseEventData> onDrag;
        public Action<BaseEventData> onEndDrag;

        public Action<BaseEventData> onPointerDown;
        public Action<BaseEventData> onPointerEnter;
        public Action<BaseEventData> onPointerExit;
        public Action<BaseEventData> onPointerUp;

        public Action<GameObject> onSelect;
        public Action<GameObject> onUpdateSelect;

        public Action<bool> onValueChanged;

        private void OnDestroy()
        {
            onClick = null;
            onSelect = null;
            onUpdateSelect = null;

            onPointerDown = null;
            onPointerEnter = null;
            onPointerExit = null;
            onPointerUp = null;

            onBeginDrag = null;
            onEndDrag = null;
            onDrag = null;

            onValueChanged = null;
        }

        public static UIEventListener CheckAndAddListener(GameObject go)
        {
            var listener = go.GetComponent<UIEventListener>();
            if (listener == null) listener = go.AddComponent<UIEventListener>();

            return listener;
        }

        public static UIEventListener Get(GameObject go)
        {
            return CheckAndAddListener(go);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null) onClick();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (onPointerDown != null) onPointerDown(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (onPointerEnter != null) onPointerEnter(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (onPointerExit != null) onPointerExit(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (onPointerUp != null) onPointerUp(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            if (onSelect != null) onSelect(gameObject);
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (onUpdateSelect != null) onUpdateSelect(gameObject);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (onBeginDrag != null) onBeginDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (onEndDrag != null) onEndDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (onDrag != null) onDrag(eventData);
        }
    }
}