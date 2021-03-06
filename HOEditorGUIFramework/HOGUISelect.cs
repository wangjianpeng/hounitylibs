﻿// Author: Daniele Giardini
// Copyright (c) 2012 Daniele Giardini - Holoville - http://www.holoville.com
// Created: 2012/11/01 12:56

using System;
using System.Collections;
using System.Collections.Generic;
using Holoville.HOEditorGUIFramework.Core;
using Holoville.HOEditorGUIFramework.Utils;
using UnityEditor;
using UnityEngine;

namespace Holoville.HOEditorGUIFramework
{
    /// <summary>
    /// Manages selection of multiple GUI elements.
    /// </summary>
    public static class HOGUISelect
    {
        // Default selection colors
        static readonly Color _DefSelectionColor = new Color(0.1720873f, 0.4236527f, 0.7686567f, 0.35f);

        static Color _selectionColor;
        static Color _unfocusedSelectionColor;
        static GUISelectData _selectData;
        static Editor _editor;
        static EditorWindow _editorWindow;

        // ===================================================================================
        // PUBLIC METHODS --------------------------------------------------------------------

        /// <summary>
        /// Call this after each selectable GUI block, to calculate and draw the current selection state.
        /// Return TRUE while the item at the given index is selected.
        /// </summary>
        /// <param name="editor">Reference to the panel calling this method (used for Repaint purposes)</param>
        /// <param name="selectableList">List containig the selectable items</param>
        /// <param name="selectionItemIndex">Index of the last selectable item block drawn</param>
        public static bool SetState(Editor editor, IList selectableList, int selectionItemIndex)
        { return SetState(editor, null, selectableList, selectionItemIndex, _DefSelectionColor); }
        /// <summary>
        /// Call this after each selectable GUI block, to calculate and draw the current selection state.
        /// Return TRUE while the item at the given index is selected.
        /// </summary>
        /// <param name="editor">Reference to the panel calling this method (used for Repaint purposes)</param>
        /// <param name="selectableList">List containig the selectable items</param>
        /// <param name="selectionItemIndex">Index of the last selectable item block drawn</param>
        /// <param name="selectionColor">Color to apply when selecting</param>
        public static bool SetState(Editor editor, IList selectableList, int selectionItemIndex, Color selectionColor)
        { return SetState(editor, null, selectableList, selectionItemIndex, selectionColor); }
        /// <summary>
        /// Call this after each selectable GUI block, to calculate and draw the current selection state.
        /// Return TRUE while the item at the given index is selected.
        /// </summary>
        /// <param name="editorWindow">Reference to the panel calling this method (used for Repaint purposes)</param>
        /// <param name="selectableList">List containig the selectable items</param>
        /// <param name="selectionItemIndex">Index of the last selectable item block drawn</param>
        public static bool SetState(EditorWindow editorWindow, IList selectableList, int selectionItemIndex)
        { return SetState(null, editorWindow, selectableList, selectionItemIndex, _DefSelectionColor); }
        /// <summary>
        /// Call this after each selectable GUI block, to calculate and draw the current selection state.
        /// Return TRUE while the item at the given index is selected.
        /// </summary>
        /// <param name="editorWindow">Reference to the panel calling this method (used for Repaint purposes)</param>
        /// <param name="selectableList">List containig the selectable items</param>
        /// <param name="selectionItemIndex">Index of the last selectable item block drawn</param>
        /// <param name="selectionColor">Color to apply when selecting</param>
        public static bool SetState(EditorWindow editorWindow, IList selectableList, int selectionItemIndex, Color selectionColor)
        { return SetState(null, editorWindow, selectableList, selectionItemIndex, selectionColor); }

        static bool SetState(Editor editor, EditorWindow editorWindow, IList selectableList, int selectionItemIndex, Color selectionColor)
        {
            if (editor != null) _editor = editor;
            else _editorWindow = editorWindow;
            _selectionColor = selectionColor;
            _selectionColor.a = 0.35f;
            _unfocusedSelectionColor = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.15f);
            if (_selectData == null || !_selectData.IsStoredList(selectableList)) _selectData = new GUISelectData(selectableList);
            
            // Check if something was deleted from the list before continuing
            if (selectionItemIndex == -1 || selectionItemIndex > _selectData.selectableItemsDatas.Count - 1) return false;

            GUISelectData.ItemData itemData = _selectData.selectableItemsDatas[selectionItemIndex];
            if (Event.current.type == EventType.Repaint) itemData.rect = GUILayoutUtility.GetLastRect();
            bool wasPressed = itemData.isPressed;
            bool selectionStatusChanged = false;
            if (Event.current.type == EventType.MouseDown) {
                itemData.isPressed = itemData.rect.Contains(Event.current.mousePosition);
                if (wasPressed != itemData.isPressed && !itemData.selected) {
                    selectionStatusChanged = true;
                    if (!itemData.selected) itemData.canBeDeselected = false;
                }
            } else if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used || Event.current.type == EventType.DragExited) {
                if (!itemData.canBeDeselected) itemData.canBeDeselected = true;
                else if (itemData.isPressed && itemData.selected && itemData.rect.Contains(Event.current.mousePosition)) selectionStatusChanged = true;
                else if (HOGUIUtils.PanelContainsMouse() && itemData.selected && !Event.current.shift && !Event.current.control && !itemData.rect.Contains(Event.current.mousePosition)) {
                    itemData.selected = false;
                    Repaint();
                }
                itemData.isPressed = false;
            }

            if (selectionStatusChanged) {
                itemData.selected = !itemData.selected;
                if (Event.current.shift) {
                    _selectData.CheckFirstSelectedItem(itemData);
                    SelectRange(_selectData.firstSelectedItemData, itemData);
                } else if (!Event.current.control) {
                    DeselectAll(itemData.selected ? itemData : null);
                    _selectData.CheckFirstSelectedItem(itemData);
                }
                Repaint();
            }

            if (itemData.selected) {
                HOGUI.FlatDivider(itemData.rect, IsFocusedPanel() ? _selectionColor : _unfocusedSelectionColor);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the currently selected indexes in the selection list,
        /// or an empty list if none is selected.
        /// </summary>
        /// <returns></returns>
        public static List<int> GetSelectedIndexes()
        {
            List<int> selIndexes = new List<int>();
            if (_selectData == null) return selIndexes;
            int len = _selectData.selectableItemsDatas.Count;
            for (int i = 0; i < len; i++) {
                if (_selectData.selectableItemsDatas[i].selected) selIndexes.Add(i);
            }
            return selIndexes;
        }

        // ===================================================================================
        // METHODS ---------------------------------------------------------------------------

        static void DeselectAll(GUISelectData.ItemData excludeItemData = null)
        {
            int len = _selectData.selectableItemsDatas.Count;
            for (int i = 0; i < len; i++) {
                GUISelectData.ItemData itemData = _selectData.selectableItemsDatas[i];
                if (itemData == excludeItemData) continue;
                itemData.selected = false;
            }
        }

        static void SelectRange(GUISelectData.ItemData rangeStart, GUISelectData.ItemData rangeEnd)
        {
            if (rangeStart.index == rangeEnd.index) return;
            if (rangeStart.index > rangeEnd.index) {
                GUISelectData.ItemData rangeTmp = rangeStart;
                rangeStart = rangeEnd;
                rangeEnd = rangeTmp;
            }
            int len = _selectData.selectableItemsDatas.Count;
            for (int i = 0; i < len; i++) {
                GUISelectData.ItemData itemData = _selectData.selectableItemsDatas[i];
                itemData.selected = itemData.index >= rangeStart.index && itemData.index <= rangeEnd.index;
            }
        }

        static bool IsFocusedPanel()
        {
            if (_editorWindow == null) return true;
            return EditorWindow.focusedWindow == _editorWindow;
        }

        static void Repaint()
        {
            if (_editor != null) _editor.Repaint();
            else _editorWindow.Repaint();
        }
    }
}