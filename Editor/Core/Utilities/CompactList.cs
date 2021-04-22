using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A list implementation optimized for large numbers of complex elements.
    /// </summary>
    /// <remarks>
    /// <see cref="ReorderableList"/> can take up lots of space when many elements are stored in the list,
    /// and does not scale in performance for complex element types. By using a scroll view for the
    /// list and only allowing editing of a single element at a time, the list is more compact and has more
    /// predictable performance. It also supports search functionality.
    /// </remarks>
    class CompactList
    {
        static readonly MethodInfo s_GetPermanentControlID = typeof(GUIUtility)
            .GetMethod("GetPermanentControlID", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly MethodInfo s_HasCurrentWindowKeyFocus = typeof(EditorGUIUtility)
            .GetMethod("HasCurrentWindowKeyFocus", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly MethodInfo s_EndEditingActiveTextField = typeof(EditorGUI)
            .GetMethod("EndEditingActiveTextField", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly MethodInfo s_ToolbarSearchField = typeof(EditorGUI)
            .GetMethod("ToolbarSearchField", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Rect), typeof(string), typeof(bool) }, null);

        static readonly float k_MinItemHeight = 2f;
        static readonly float k_MinListHeight = 5f * EditorGUIUtility.singleLineHeight;

        class Defaults
        {
            public const float boxPadding = 1f;
            public readonly GUIStyle boxHeaderBackground = "RL Empty Header";
            public readonly GUIStyle boxBackground = "RL Background";

            public const float itemPadding = 6f;
            public readonly GUIStyle itemBackground = "RL Element";

            public const float resizeHandleHeight = 12f;
            public readonly GUIStyle resizeHandle = "RL DragHandle";

            public const float searchBarHeight = 18f;
            public const float searchBottomPadding = 2f;
            public const float searchSidePadding = 6f;
            public readonly GUIContent showSearchbar = EditorGUIUtility.TrTextContent("Show Searchbar");

            public const float controlsSpacing = 2f;
            public const float buttonHeight = 15f;
            public const float buttonWidth = 20f;
            public const float buttonSpacing = 8f;
            public readonly GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to list");
            public readonly GUIContent iconToolbarPlusMore = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose to add to list");
            public readonly GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection or last element from list");
            public readonly GUIContent menuButton = EditorGUIUtility.TrIconContent("_Menu", "List menu");
            public readonly GUIContent moveUpIcon = EditorGUIUtility.TrTextContent("\u25B2", "Move selection up");
            public readonly GUIContent moveDownIcon = EditorGUIUtility.TrTextContent("\u25BC", "Move selection down");
            public readonly GUIStyle controlButton = new GUIStyle("RL FooterButton")
            {
                fontSize = 10,
            };

            static GUIContent s_TempContent = new GUIContent();

            static GUIContent TempContent(string t)
            {
                s_TempContent.image = null;
                s_TempContent.text = t;
                s_TempContent.tooltip = null;
                return s_TempContent;
            }

            public void DoAddButton(CompactList list)
            {
                if (list.property != null)
                {
                    list.property.arraySize += 1;
                    list.index = list.property.arraySize - 1;
                }
                else
                {
                    var elementType = list.list.GetType().GetElementType();

                    if (elementType == typeof(string))
                    {
                        list.index = list.list.Add(string.Empty);
                    }
                    else if (elementType != null && elementType.GetConstructor(Type.EmptyTypes) == null)
                    {
                        Debug.LogError($"Cannot add element. Type {elementType} has no default constructor. Implement a default constructor or implement your own add behaviour.");
                    }
                    else if (list.list.GetType().GetGenericArguments()[0] != null)
                    {
                        list.index = list.list.Add(Activator.CreateInstance(list.list.GetType().GetGenericArguments()[0]));
                    }
                    else if (elementType != null)
                    {
                        list.index = list.list.Add(Activator.CreateInstance(elementType));
                    }
                    else
                    {
                        Debug.LogError("Cannot add element of type Null.");
                    }
                }
            }

            public void DoRemoveButton(CompactList list)
            {
                if (list.property != null)
                {
                    list.property.DeleteArrayElementAtIndex(list.index);

                    if (list.index >= list.property.arraySize - 1)
                        list.index = list.property.arraySize - 1;
                }
                else
                {
                    list.list.RemoveAt(list.index);

                    if (list.index >= list.list.Count - 1)
                        list.index = list.list.Count - 1;
                }
            }

            public bool DoSearchFilter(CompactList list, int index, string searchFilter)
            {
                var filter = searchFilter.ToLowerInvariant();
                var value = GetItemName(list, index).ToLowerInvariant();
                return value.Contains(filter);
            }

            public void DrawListBackground(Rect rect)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var topRect = new Rect(rect)
                    {
                        yMax = rect.y + 5f,
                    };
                    var bottomRect = new Rect(rect)
                    {
                        yMin = rect.y + 5f,
                    };

                    boxHeaderBackground.Draw(topRect, false, false, false, false);
                    boxBackground.Draw(bottomRect, false, false, false, false);
                }
            }

            public void DrawListItemBackground(Rect rect, int index, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    itemBackground.Draw(rect, false, selected, selected, focused);
                }
            }

            public void DrawListItem(CompactList list, Rect rect, int index, bool selected, bool focused)
            {
                EditorGUI.LabelField(rect, TempContent(GetItemName(list, index)));
            }

            string GetItemName(CompactList list, int index)
            {
                if (list.property != null)
                {
                    var element = list.property.GetArrayElementAtIndex(index);
                    return element.displayName;
                }
                else
                {
                    var element = list.list[index];
                    return element.ToString();
                }
            }
        }

        static Defaults defaults { get; set; }

        readonly int m_ID;
        readonly string m_StatePath;

        int m_Index;
        int m_ScrollToElement = -1;
        float m_ScrollPosition;
        bool m_Resizing;
        float m_ResizeStartOffset;
        float m_ResizeStartHeight;
        bool m_ShowSearchBar = true;
        string m_SearchFilter = string.Empty;
        float m_ItemHeight = EditorGUIUtility.singleLineHeight;
        float m_ListHeight = 5f * EditorGUIUtility.singleLineHeight;
        readonly Bijection<int, int> m_SearchElementToItem = new Bijection<int, int>();

        /// <summary>
        /// The serialized property being edited by instance.
        /// </summary>
        /// <value>
        /// If null, <see cref="list"/> is being used instead.
        /// </value>
        public SerializedProperty property { get; }

        /// <summary>
        /// The list being edited by this instance.
        /// </summary>
        /// <value>
        /// If null, <see cref="property"/> is being used instead.
        /// </value>
        public IList list { get; }

        /// <summary>
        /// The number of elements in the list.
        /// </summary>
        /// <remarks>
        /// If <see cref="property"/> is non-null and multiple target objects are being edited,
        /// this will be the minimum array size out of all the target objects.
        /// </remarks>
        public int count
        {
            get
            {
                if (property != null)
                {
                    if (!property.hasMultipleDifferentValues)
                        return property.arraySize;

                    var minArraySize = property.arraySize;
                    foreach (var targetObject in property.serializedObject.targetObjects)
                    {
                        using (var serializedObject = new SerializedObject(targetObject))
                        {
                            var property = serializedObject.FindProperty(this.property.propertyPath);
                            minArraySize = Math.Min(property.arraySize, minArraySize);
                        }
                    }
                    return minArraySize;
                }
                return list.Count;
            }
        }

        /// <summary>
        /// The index of the selected element in the list.
        /// </summary>
        public int index
        {
            get => m_Index;
            set
            {
                if (m_Index != value)
                {
                    m_Index = value;
                    SessionState.SetInt($"{m_StatePath}/index", m_Index);
                    ScrollToItem(m_Index);
                }
            }
        }

        /// <summary>
        /// Can the list size be changed by dragging the footer.
        /// </summary>
        public bool resizable { get; set; } = true;

        /// <summary>
        /// Can the items in the list be reordered.
        /// </summary>
        public bool reorderable { get; set; } = true;

        /// <summary>
        /// Can the list items be searched.
        /// </summary>
        public bool searchable { get; set; } = true;

        /// <summary>
        /// Expand the search bar.
        /// </summary>
        public bool showSearchBar
        {
            get => m_ShowSearchBar;
            set
            {
                if (m_ShowSearchBar != value)
                {
                    m_ShowSearchBar = value;
                    SessionState.SetBool($"{m_StatePath}/showSearchBar", m_ShowSearchBar);
                }
            }
        }

        /// <summary>
        /// The search string used to filter the list items.
        /// </summary>
        public string searchFilter
        {
            get => m_SearchFilter;
            set
            {
                if (m_SearchFilter != value)
                {
                    m_SearchFilter = value ?? string.Empty;
                    SessionState.SetString($"{m_StatePath}/searchFilter", m_SearchFilter);
                }
            }
        }

        /// <summary>
        /// Is there a search filter currently being applied to the list.
        /// </summary>
        public bool isSearchActive => searchable && !string.IsNullOrEmpty(searchFilter);

        /// <summary>
        /// The height of items in the list in pixels.
        /// </summary>
        public float itemHeight
        {
            get => m_ItemHeight;
            set
            {
                if (m_ItemHeight != value)
                {
                    m_ItemHeight = Mathf.Max(k_MinItemHeight, value);
                    ScrollToItem(index);
                }
            }
        }

        /// <summary>
        /// The height of the list item area in pixels.
        /// </summary>
        public float listHeight
        {
            get => m_ListHeight;
            set
            {
                if (m_ListHeight != value)
                {
                    m_ListHeight = Mathf.Max(k_MinListHeight, value);
                    SessionState.SetFloat($"{m_StatePath}/listHeight", m_ListHeight);
                    ScrollToItem(index);
                }
            }
        }

        /// <summary>
        /// A callback used to determine if the user is able to add items to the list.
        /// </summary>
        /// <remarks>
        /// <para>Return true if elements can be added to the list; false otherwise.</para>
        /// </remarks>
        public Func<bool> onCanAddCallback;

        /// <summary>
        /// A callback used to determine if the user is able to remove items from the list.
        /// </summary>
        /// <remarks>
        /// <para>Return true if elements can be removed from the list; false otherwise.</para>
        /// </remarks>
        public Func<bool> onCanRemoveCallback;

        /// <summary>
        /// A callback used to override how items are added to the list.
        /// </summary>
        public Action onAddCallback;

        /// <summary>
        /// A callback used to display advanced options when the add button is clicked.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the add button position.</para>
        /// </remarks>
        public Action<Rect> onAddDropdownCallback;

        /// <summary>
        /// A callback used to override how items are removed from the list.
        /// </summary>
        public Action onRemoveCallback;

        /// <summary>
        /// A callback invoked after the user has clicked on the add or remove buttons.
        /// </summary>
        public Action onChangedCallback;

        /// <summary>
        /// A callback used to add a custom menu to the list controls.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the menu to add items to.</para>
        /// </remarks>
        public Action<GenericMenu> onMenuCallback;

        /// <summary>
        /// A callback invoked when the user moves an item to a new index in the list.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the previous index of the moved item.</para>
        /// <para>The second parameter contains the new index of the moved item.</para>
        /// </remarks>
        public Action<int, int> onReorderCallback;

        /// <summary>
        /// A callback invoked when the user selects an item in the list.
        /// </summary>
        public Action onSelectCallback;

        /// <summary>
        /// A callback invoked when the user releases a click on the selected item in the list.
        /// </summary>
        public Action onMouseUpCallback;

        /// <summary>
        /// A callback invoked when the user changes the list size.
        /// </summary>
        public Action onResizeCallback;

        /// <summary>
        /// A callback that overrides the search filter behaviour.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the index of the item to filter.</para>
        /// <para>The second parameter contains the search string.</para>
        /// <para>Return true to show the item in the filtered list, false to reject it.</para>
        /// </remarks>
        public Func<int, string, bool> searchFilterCallback;

        /// <summary>
        /// A callback that overrides the height of the GUI for the selected element.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the index of the element to get the height of.</para>
        /// <para>Return the height of the element in pixels.</para>
        /// </remarks>
        public Func<int, float> elementHeightCallback;

        /// <summary>
        /// A callback that overrides the background for items in the list box.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is only called for items that are currently visible in the list box.
        /// </para>
        /// <para>The first parameter contains the position to draw the item in.</para>
        /// <para>The second parameter contains the index of the item being drawn.</para>
        /// <para>The third parameter indicates if this item is currently selected.</para>
        /// <para>The fourth parameter indicates if this item is currently selected and has keyboard focus.</para>
        /// </remarks>
        public Action<Rect, int, bool, bool> drawListItemBackgroundCallback;

        /// <summary>
        /// A callback that overrides the GUI for items in the list box.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is only called for items that are currently visible in the list box.
        /// </para>
        /// <para>The first parameter contains the position to draw the item in.</para>
        /// <para>The second parameter contains the index of the item being drawn.</para>
        /// <para>The third parameter indicates if this item is currently selected.</para>
        /// <para>The fourth parameter indicates if this item is currently selected and has keyboard focus.</para>
        /// </remarks>
        public Action<Rect, int, bool, bool> drawListItemCallback;

        /// <summary>
        /// A callback that overrides the GUI for the selected element that is drawn beneath the list.
        /// </summary>
        /// <remarks>
        /// <para>The first parameter contains the position to draw the element GUI in.</para>
        /// <para>The second parameter contains the index of the element to draw the GUI for.</para>
        /// </remarks>
        public Action<Rect, int> drawElementCallback;

        /// <summary>
        /// Creates a new <see cref="CompactList"/> instance.
        /// </summary>
        /// <param name="elements">The array property to edit.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elements"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="elements"/> is not an array property.</exception>
        public CompactList(SerializedProperty elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (!elements.isArray)
                throw new ArgumentException($"Must be an array {nameof(SerializedProperty)}!", nameof(elements));

            m_ID = (int)s_GetPermanentControlID.Invoke(null, null);
            property = elements;

            m_StatePath = $"{elements.serializedObject.targetObject.GetInstanceID()}/{elements.propertyPath}";
            LoadState();
        }

        /// <summary>
        /// Creates a new <see cref="CompactList"/> instance.
        /// </summary>
        /// <param name="elements">The list to edit.</param>
        /// <param name="statePath">The unique path used to store the list state in <see cref="SessionState"/>.
        /// It must not be null or empty and should not conflict with other lists.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elements"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="statePath"/> is null or empty.</exception>
        public CompactList(IList elements, string statePath)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (string.IsNullOrWhiteSpace(statePath))
                throw new ArgumentException("Path \"{statePath}\" is not valid!", nameof(statePath));

            m_ID = (int)s_GetPermanentControlID.Invoke(null, null);
            list = elements;

            m_StatePath = statePath;
            LoadState();
        }

        void LoadState()
        {
            index = SessionState.GetInt($"{m_StatePath}/index", index);
            showSearchBar = SessionState.GetBool($"{m_StatePath}/showSearchBar", showSearchBar);
            searchFilter = SessionState.GetString($"{m_StatePath}/searchFilter", searchFilter);
            m_ScrollPosition = SessionState.GetFloat($"{m_StatePath}/scrollPosition", m_ScrollPosition);
            listHeight = SessionState.GetFloat($"{m_StatePath}/listHeight", listHeight);
        }

        /// <summary>
        /// Gets the height of the list.
        /// </summary>
        /// <returns>The height in pixels.</returns>
        public float GetHeight()
        {
            return GetListBoxHeight() + EditorGUIUtility.standardVerticalSpacing + GetElementHeight();
        }

        float GetListBoxHeight()
        {
            return GetListItemsHeight() + GetListFooterHeight();
        }

        float GetListItemsHeight()
        {
            return listHeight + (2f * Defaults.boxPadding);
        }

        float GetListFooterHeight()
        {
            var height = 0f;

            if (resizable)
                height += Defaults.resizeHandleHeight;
            if (searchable && showSearchBar)
                height += Defaults.searchBarHeight + Defaults.searchBottomPadding;

            return height;
        }

        float GetElementHeight()
        {
            if (index < 0 || index >= count)
                return 0f;

            if (elementHeightCallback == null)
            {
                if (property != null)
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(element);
                }
                return 0f;
            }
            return elementHeightCallback(index);
        }

        /// <summary>
        /// Draws the list using the layout system.
        /// </summary>
        public void DoGUILayout()
        {
            var listRect = GUILayoutUtility.GetRect(0, GetListBoxHeight(), GUILayout.ExpandWidth(true));

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            var elementRect = GUILayoutUtility.GetRect(0, GetElementHeight(), GUILayout.ExpandWidth(true));

            DoListInternal(listRect, elementRect);
        }

        /// <summary>
        /// Draws the list.
        /// </summary>
        /// <param name="rect">The rect to draw the list in.</param>
        public void DoGUI(Rect rect)
        {
            var listRect = new Rect()
            {
                x = rect.x,
                y = rect.y,
                width = rect.width,
                height = GetListBoxHeight(),
            };
            var elementRect = new Rect()
            {
                x = rect.x,
                y = listRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                width = rect.width,
                height = GetElementHeight(),
            };

            DoListInternal(listRect, elementRect);
        }

        void DoListInternal(Rect listRect, Rect elementRect)
        {
            Profiler.BeginSample($"{nameof(CompactList)}.{nameof(DoListInternal)}()");

            if (defaults == null)
                defaults = new Defaults();

            DoList(listRect);
            DoElement(elementRect);

            Profiler.EndSample();
        }

        void DoList(Rect rect)
        {
            var controlsRect = new Rect(rect)
            {
                xMin = rect.xMax - Defaults.buttonWidth,
            };

            DoListControls(controlsRect);

            var listRect = new Rect(rect)
            {
                xMax = controlsRect.xMin - Defaults.controlsSpacing,
            };
            var itemsRect = new Rect(listRect)
            {
                height = GetListItemsHeight(),
            };
            var footerRect = new Rect(listRect)
            {
                y = itemsRect.yMax,
                height = GetListFooterHeight(),
            };

            defaults.DrawListBackground(listRect);

            // we need to draw the search field before the items, to prevent the control IDs from changing during the search
            DoListFooter(footerRect);
            DoListItems(itemsRect);
        }

        void DoListControls(Rect rect)
        {
            var addRect = new Rect(rect)
            {
                height = Defaults.buttonHeight,
            };
            var removeRect = new Rect(addRect)
            {
                y = addRect.yMax,
            };
            var menuRect = new Rect(removeRect)
            {
                y = removeRect.yMax + Defaults.buttonSpacing,
            };
            var upRect = new Rect(menuRect)
            {
                y = menuRect.yMax + Defaults.buttonSpacing,
            };
            var downRect = new Rect(upRect)
            {
                y = upRect.yMax,
            };

            var indexInvalid = index < 0 || index >= count;

            using (new EditorGUI.DisabledScope(onCanAddCallback != null && !onCanAddCallback()))
            {
                if (GUI.Button(addRect, onAddDropdownCallback != null ? defaults.iconToolbarPlusMore : defaults.iconToolbarPlus, defaults.controlButton))
                {
                    if (onAddDropdownCallback != null)
                    {
                        onAddDropdownCallback(addRect);
                    }
                    else if (onAddCallback == null)
                    {
                        defaults.DoAddButton(this);
                    }
                    else
                    {
                        onAddCallback();
                    }

                    onChangedCallback?.Invoke();
                }
            }

            using (new EditorGUI.DisabledScope(indexInvalid || (onCanRemoveCallback != null && !onCanRemoveCallback())))
            {
                if (GUI.Button(removeRect, defaults.iconToolbarMinus, defaults.controlButton))
                {
                    if (onRemoveCallback == null)
                    {
                        defaults.DoRemoveButton(this);
                    }
                    else
                    {
                        onRemoveCallback();
                    }

                    onChangedCallback?.Invoke();
                }
            }

            if (onMenuCallback != null || searchable)
            {
                if (GUI.Button(menuRect, defaults.menuButton, defaults.controlButton))
                {
                    var menu = new GenericMenu();

                    if (onMenuCallback != null)
                    {
                        onMenuCallback(menu);
                    }
                    else if (searchable)
                    {
                        menu.AddItem(defaults.showSearchbar, showSearchBar, () =>
                        {
                            showSearchBar = !showSearchBar;
                        });
                    }

                    menu.DropDown(menuRect);
                }
            }

            if (reorderable)
            {
                using (new EditorGUI.DisabledScope(indexInvalid || count < 2))
                {
                    if (GUI.Button(upRect, defaults.moveUpIcon, defaults.controlButton))
                    {
                        MoveSelectionUp();
                    }
                    if (GUI.Button(downRect, defaults.moveDownIcon, defaults.controlButton))
                    {
                        MoveSelectionDown();
                    }
                }
            }
        }

        void MoveSelectionUp()
        {
            var arraySize = count;
            var dstIndex = (index - 1 + arraySize) % arraySize;
            MoveSelection(dstIndex);
        }

        void MoveSelectionDown()
        {
            var arraySize = count;
            var dstIndex = (index + 1 + arraySize) % arraySize;
            MoveSelection(dstIndex);
        }

        void MoveSelection(int dstIndex)
        {
            var srcIndex = index;

            if (property != null)
            {
                property.MoveArrayElement(srcIndex, dstIndex);
            }
            else
            {
                var value = list[srcIndex];
                list.RemoveAt(srcIndex);
                list.Insert(dstIndex, value);
            }

            index = dstIndex;

            onReorderCallback?.Invoke(srcIndex, dstIndex);
        }

        void DoListItems(Rect rect)
        {
            Profiler.BeginSample($"{nameof(CompactList)}.{nameof(DoListItems)}()");

            var itemCount = FilterList();

            // don't draw over the list box border
            var scrollRect = rect;
            scrollRect.min += Vector2.one * Defaults.boxPadding;
            scrollRect.max -= Vector2.one * Defaults.boxPadding;

            // determine how much space is needed for the list
            var listRect = scrollRect;
            listRect.height = itemHeight * itemCount;

            // if the list is larger the scroll box we need to make room for the scroll bar
            var showScrollbar = listRect.yMax > scrollRect.yMax;

            if (showScrollbar)
                listRect.xMax -= 16f;

            // the list items are scrollable
            using (var scroll = new GUI.ScrollViewScope(scrollRect, new Vector2(0, m_ScrollPosition), listRect, false, showScrollbar))
            {
                // find how much content is not visible
                var viewHeight = scrollRect.height;
                var contentHeight = listRect.height;
                var excessHeight = contentHeight - viewHeight;

                // find the bounds of the currently visible area
                var normalizedScroll = m_ScrollPosition / excessHeight;
                var viewBottom = viewHeight + (excessHeight * normalizedScroll);
                var viewTop = viewBottom - viewHeight;

                // draw the items
                for (var i = 0; i < itemCount; i++)
                {
                    // clip if the rect is not visible in the scroll window
                    if (IsItemClipped(viewBottom, viewTop, i))
                        continue;

                    var itemRect = GetItemRect(listRect, i);

                    // get the original element index
                    var idx = isSearchActive ? m_SearchElementToItem.reverse[i] : i;
                    var activeElement = (idx == index);
                    var focusedElement = (idx == index && HasKeyboardControl());

                    // draw the background
                    if (drawListItemBackgroundCallback == null)
                    {
                        defaults.DrawListItemBackground(itemRect, idx, activeElement, focusedElement);
                    }
                    else
                    {
                        drawListItemBackgroundCallback(itemRect, idx, activeElement, focusedElement);
                    }

                    // draw the list item
                    var itemContentRect = new Rect(itemRect)
                    {
                        xMin = itemRect.xMin + Defaults.itemPadding,
                        xMax = itemRect.xMax - Defaults.itemPadding,
                    };

                    if (drawListItemCallback == null)
                    {
                        defaults.DrawListItem(this, itemContentRect, idx, activeElement, focusedElement);
                    }
                    else
                    {
                        drawListItemCallback(itemContentRect, idx, activeElement, focusedElement);
                    }
                }

                ProcessListItemInteraction(listRect, itemCount);

                var lastScrollPosition = m_ScrollPosition;
                m_ScrollPosition = scroll.scrollPosition.y;

                if (m_ScrollToElement >= 0)
                {
                    ScrollToElement(viewHeight, viewBottom, viewTop, m_ScrollToElement);
                    m_ScrollToElement = -1;
                }

                if (m_ScrollPosition != lastScrollPosition)
                    SessionState.SetFloat($"{m_StatePath}/scrollPosition", m_ScrollPosition);
            }

            Profiler.EndSample();
        }

        int FilterList()
        {
            if (!isSearchActive)
                return count;

            Profiler.BeginSample($"{nameof(CompactList)}.{nameof(FilterList)}()");

            // If there is a search active we must find the mapping between filtered items shown
            // in the list and the original elements in the list.
            m_SearchElementToItem.Clear();

            for (var i = 0; i < count; i++)
            {
                bool passesFilter;

                if (searchFilterCallback == null)
                {
                    passesFilter = defaults.DoSearchFilter(this, i, searchFilter);
                }
                else
                {
                    passesFilter = searchFilterCallback(i, searchFilter);
                }

                if (passesFilter)
                {
                    m_SearchElementToItem.Add(i, m_SearchElementToItem.Count);
                }
            }

            Profiler.EndSample();

            return m_SearchElementToItem.Count;
        }

        void ProcessListItemInteraction(Rect listRect, int itemCount)
        {
            var evt = Event.current;
            var lastIndex = index;
            var clicked = false;

            switch (evt.GetTypeForControl(m_ID))
            {
                case EventType.KeyDown:
                {
                    // if we have keyboard focus, arrow through the list
                    if (GUIUtility.keyboardControl != m_ID)
                        return;

                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        // find the next item that passes the search filter, if any
                        if (isSearchActive)
                        {
                            for (var i = Mathf.Max(0, index + 1); i < count; i++)
                            {
                                if (m_SearchElementToItem.ContainsKey(i))
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            index = Mathf.Clamp(index + 1, 0, itemCount - 1);
                        }
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        // find the previous item that passes the search filter, if any
                        if (isSearchActive)
                        {
                            for (var i = Mathf.Min(count - 1, index - 1); i >= 0; i--)
                            {
                                if (m_SearchElementToItem.ContainsKey(i))
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            index = Mathf.Clamp(index - 1, 0, itemCount - 1);
                        }
                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseDown:
                {
                    if (!listRect.Contains(evt.mousePosition) || (evt.button != 0 && evt.button != 1))
                        break;

                    // clicking on the list should end editing any existing edits
                    s_EndEditingActiveTextField.Invoke(null, null);

                    GrabKeyboardFocus();

                    // do not auto-scroll to the new selection, it can be confusing to the user
                    var item = GetItemUnderMouse(listRect, itemCount);
                    m_Index = isSearchActive ? m_SearchElementToItem.reverse[item] : item;;
                    SessionState.SetInt($"{m_StatePath}/index", m_Index);

                    // Prevent consuming the right mouse event in order to enable context menus
                    if (evt.button == 1)
                        break;

                    evt.Use();
                    clicked = true;
                    break;
                }
                case EventType.MouseUp:
                {
                    // if mouse up was on the same index as mouse down we fire a mouse up callback
                    // (useful if for beginning renaming on mouseup and so on)
                    if (onMouseUpCallback != null)
                    {
                        var item = GetItemUnderMouse(listRect, itemCount);

                        if (item == index && GetItemRect(listRect, item).Contains(evt.mousePosition))
                        {
                            onMouseUpCallback();
                        }
                    }
                    break;
                }
            }

            // if the index has changed and there is a selected callback, call it
            if (onSelectCallback != null && (index != lastIndex || clicked))
            {
                onSelectCallback();
            }
        }

        Rect GetItemRect(Rect listRect, int item)
        {
            return new Rect(listRect.x, listRect.y + (itemHeight * item), listRect.width, itemHeight);
        }

        int GetItemUnderMouse(Rect listRect, int itemCount)
        {
            var viewY = Event.current.mousePosition.y - listRect.y;
            return Mathf.Clamp(Mathf.FloorToInt(viewY / itemHeight), 0, itemCount - 1);
        }

        bool IsItemClipped(float viewBottom, float viewTop, int itemIndex)
        {
            var itemTop = itemHeight * itemIndex;

            if (itemTop > viewBottom + 1f)
                return true;

            var itemBottom = itemTop + itemHeight;

            if (itemBottom < viewTop - 1f)
                return true;

            return false;
        }

        void ScrollToElement(float viewHeight, float viewBottom, float viewTop, int elementIndex)
        {
            // only scroll to valid elements
            if (elementIndex < 0 || elementIndex >= count)
                return;

            // find the item corresponding to the target element
            var itemIndex = elementIndex;

            // do not scroll to elements that are filtered out by the search
            if (isSearchActive && !m_SearchElementToItem.forward.TryGetValue(itemIndex, out itemIndex))
                return;

            // find the bounds of the selected item
            var itemTop = itemHeight * itemIndex;
            var itemBottom = itemTop + itemHeight;

            // Constrain how close to the edge of the viewport the selected item can be,
            // unless the selection is near the beginning or end of the list.
            // A view margin of 0 will adjust the view to only include the selected item,
            // while larger values will show additional values past the selection in the
            // scroll direction. Each multiple reveals an additional item.
            const float viewMargin = 1f;
            var margin = Mathf.Min(itemHeight * viewMargin, (viewHeight - itemHeight) / 2f);

            var adjustedTop = itemTop - margin;
            var adjustedBottom = itemBottom + margin;

            float desiredViewBottom;
            if (adjustedTop < viewTop)
            {
                desiredViewBottom = adjustedTop + viewHeight;
            }
            else if (adjustedBottom > viewBottom)
            {
                desiredViewBottom = adjustedBottom;
            }
            else
            {
                return;
            }

            // find the scroll position that shows the desired view
            m_ScrollPosition = desiredViewBottom - viewHeight;
        }

        void DoListFooter(Rect rect)
        {
            if (resizable)
            {
                // draw a handle icon to signify the list is resizable to the center
                var resizeHandleRect = new Rect(rect)
                {
                    xMin = rect.center.x - 10f,
                    xMax = rect.center.x + 10f,
                    y = rect.y + 3f,
                };

                if (Event.current.type == EventType.Repaint)
                {
                    defaults.resizeHandle.Draw(resizeHandleRect, false, false, false, false);
                }

                // dragging anywhere near the bottom of the list will start resizing
                var resizeRect = new Rect(rect)
                {
                    height = Defaults.resizeHandleHeight,
                };

                // when resizing the list always show the resize cursor even if not over the hot spot
                var alwaysRect = new Rect(Vector2.zero, Vector2.one * 100000f);
                EditorGUIUtility.AddCursorRect(m_Resizing ? alwaysRect : resizeRect, MouseCursor.ResizeVertical);

                ProcessResizeInteraction(resizeRect);
            }

            if (searchable && showSearchBar)
            {
                var searchBarRect = new Rect(rect)
                {
                    xMin = rect.xMin + Defaults.searchSidePadding,
                    xMax = rect.xMax - Defaults.searchSidePadding,
                    y = rect.yMax - (Defaults.searchBarHeight + Defaults.searchBottomPadding),
                    height = Defaults.searchBarHeight,
                };

                searchFilter = s_ToolbarSearchField?.Invoke(null, new object[] { searchBarRect, searchFilter, false }) as string;
            }
        }

        void ProcessResizeInteraction(Rect resizeRect)
        {
            var evt = Event.current;

            switch (evt.GetTypeForControl(m_ID))
            {
                case EventType.KeyDown:
                {
                    if (GUIUtility.keyboardControl != m_ID)
                        return;

                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == m_ID)
                    {
                        GUIUtility.hotControl = 0;
                        m_Resizing = false;

                        if (listHeight != m_ResizeStartHeight)
                        {
                            listHeight = m_ResizeStartHeight;
                            onResizeCallback?.Invoke();
                        }

                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseDown:
                {
                    if (!resizeRect.Contains(evt.mousePosition) || (evt.button != 0 && evt.button != 1))
                        break;

                    // clicking on the list should end editing any existing edits
                    s_EndEditingActiveTextField.Invoke(null, null);

                    GrabKeyboardFocus();

                    if (evt.button == 0)
                    {
                        GUIUtility.hotControl = m_ID;
                        m_Resizing = true;
                        m_ResizeStartOffset = evt.mousePosition.y;
                        m_ResizeStartHeight = listHeight;
                    }

                    // Prevent consuming the right mouse event in order to enable the context menu
                    if (evt.button == 1)
                        break;

                    evt.Use();
                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != m_ID)
                        break;

                    var targetHeight = m_ResizeStartHeight + (evt.mousePosition.y - m_ResizeStartOffset);
                    var snappedHeight = Mathf.RoundToInt(targetHeight / itemHeight) * itemHeight;

                    if (listHeight != snappedHeight)
                    {
                        listHeight = snappedHeight;
                        onResizeCallback?.Invoke();
                    }

                    evt.Use();
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != m_ID)
                        break;

                    GUIUtility.hotControl = 0;
                    m_Resizing = false;

                    evt.Use();
                    break;
                }
            }
        }

        void DoElement(Rect rect)
        {
            if (index < 0 || index >= count)
                return;

            if (drawElementCallback == null)
            {
                if (property != null)
                {
                    var element = property.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, true);
                }
            }
            else
            {
                drawElementCallback(rect, index);
            }
        }

        /// <summary>
        /// Scrolls the list to so that the item at the given index is visible to the user.
        /// </summary>
        /// <param name="index">The index of the element to scroll to.</param>
        public void ScrollToItem(int index)
        {
            if (index < 0 || index >= count)
                return;

            m_ScrollToElement = index;
        }

        /// <summary>
        /// Checks if the keyboard is currently focused on the list.
        /// </summary>
        /// <remarks>
        /// When the list has focus, the user is able to use arrow keys to change the active element.
        /// </remarks>
        /// <returns>True if the list is focused; false otherwise.</returns>
        public bool HasKeyboardControl()
        {
            return GUIUtility.keyboardControl == m_ID && (bool)s_HasCurrentWindowKeyFocus.Invoke(null, null);
        }

        /// <summary>
        /// Focuses the list control.
        /// </summary>
        /// <remarks>
        /// When the list has focus, the user is able to use arrow keys to change the active element.
        /// </remarks>
        public void GrabKeyboardFocus()
        {
            GUIUtility.keyboardControl = m_ID;
        }

        /// <summary>
        /// Releases the keyboard focus if the list is currently holding the focus.
        /// </summary>
        public void ReleaseKeyboardFocus()
        {
            if (GUIUtility.keyboardControl == m_ID)
                GUIUtility.keyboardControl = 0;
        }
    }
}
