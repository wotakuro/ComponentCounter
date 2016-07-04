using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class ComponentCountDebugWindow : EditorWindow{

    public enum ESortType
    {
        SortByAll,
        SortByActive,
        SortByName,
    }

    public enum EOutputList
    {
        AllObject,
        ActiveOnly,
        NotActiveOnly,
    }
    
    public enum ESearchMode
    {
        MonoBehaviourOnly,
        ComponentAll,
    }

    public class ComponentCount {
        public Type type;
        public int activeCnt;
        public int allCnt;

        public string typeStr { private set; get; }

        public ComponentCount(Type t)
        {
            this.type = t;
            this.activeCnt = 0;
            this.allCnt = 0;
            typeStr = t.ToString();
        }
        public ComponentCount(ComponentCount src)
        {
            this.type = src.type;
            this.activeCnt = src.activeCnt;
            this.allCnt = src.allCnt;
            typeStr = src.typeStr;
        }
    }

    /// <summary>
    /// Number of GameObject when snapshot
    /// </summary>
    private int _snapObjectNum = 0;
    /// <summary>
    /// Active Gameobject number.
    /// </summary>
    private int _activeObjectNum = 0;

    /// <summary>
    /// type list for sort
    /// </summary>
    private List<ComponentCount> _typeCntList = new List<ComponentCount>();

    /// <summary>
    /// GameObject which has selected component;
    /// </summary>
    private List<GameObject> _hasComponentGameObjectList;

    /// <summary>
    /// currentSelectTye
    /// </summary>
    private Type _currentSelectType;


    /// <summary>
    /// Scroll Position
    /// </summary>
    private Vector2 _scrollPos;

    /// <summary>
    /// Scroll Position
    /// </summary>
    private Vector2 _scrollGameObjectListPos;

    /// <summary>
    /// sort type
    /// </summary>
    private ESortType _sortType;

    /// <summary>
    /// output type
    /// </summary>
    private EOutputList _outputType;

    /// <summary>
    /// search mode
    /// </summary>
    private ESearchMode _searchMode = ESearchMode.ComponentAll;

    /// <summary>
    /// serach component filter
    /// </summary>
    private string _searchComponentFilter = "";

    /// <summary>
    /// GetWindow
    /// </summary>
    [MenuItem("Window/ComponentCountDebug")]
    public static void GetWindow()
    {
        ComponentCountDebugWindow window = EditorWindow.GetWindow<ComponentCountDebugWindow>();
        window.minSize = new Vector2(200.0f, 400.0f);
    }

    /// <summary>
    /// Search 
    /// </summary>
    void OnGUI()
    {

        _searchComponentFilter = EditorGUILayout.TextField("TypeFilter", _searchComponentFilter);
        EditorGUILayout.BeginHorizontal();
        var oldSearchMode = _searchMode;
        _searchMode = (ESearchMode)(EditorGUILayout.EnumPopup(_searchMode));
        if (oldSearchMode != _searchMode)
        {
            this.Clear();
            List<GameObject> parentObj = this.GetAllRootGameObjects();
            EditorApplication.isPaused = true;
            CreateTypeDictionary(parentObj);
        }
        if (GUILayout.Button("Search",GUILayout.Width(150) ))
        {
            List<GameObject> parentObj = this.GetAllRootGameObjects();
            EditorApplication.isPaused = true;
            CreateTypeDictionary(parentObj);
        }
        EditorGUILayout.EndHorizontal();
        // EditorGUILayout.HelpBox("If root gameObject is disabled, couldn't count child objects..", MessageType.Info);
        EditorGUILayout.HelpBox("If you don't save scene, this doesn't work..", MessageType.Info);

        EditorGUILayout.LabelField("GameObject " + _snapObjectNum);
        EditorGUILayout.LabelField("ActiveNum " + _activeObjectNum);

        EditorGUILayout.LabelField("");
        var oldSortTYpe = _sortType;
        _sortType = (ESortType)EditorGUILayout.EnumPopup(_sortType);
        if (oldSortTYpe != _sortType)
        {
            this.SortComponents();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type");
        EditorGUILayout.LabelField("active", GUILayout.Width(50));
        EditorGUILayout.LabelField("all", GUILayout.Width(50));
        EditorGUILayout.LabelField("", GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));
        if (_typeCntList != null)
        {
            foreach (var type in _typeCntList)
            {
                if (type == null) { continue; }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(type.type.ToString()))
                {
                    _currentSelectType = type.type;
                    this._hasComponentGameObjectList = GetHaveComponentObjects(this.GetAllRootGameObjects(), type.type);
                }
                EditorGUILayout.LabelField(type.activeCnt.ToString(),GUILayout.Width(50) );
                EditorGUILayout.LabelField(type.allCnt.ToString() , GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
        /// gameobject list;
        if (_hasComponentGameObjectList != null && _currentSelectType != null ) {
            EditorGUILayout.LabelField("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_currentSelectType.ToString() + " Compoent Objects");
            _outputType = (EOutputList)(EditorGUILayout.EnumPopup(_outputType , GUILayout.Width(120) ));
            EditorGUILayout.EndHorizontal();

            _scrollGameObjectListPos = EditorGUILayout.BeginScrollView(_scrollGameObjectListPos  );
            foreach( var gmo in _hasComponentGameObjectList){
                if (gmo)
                {
                    switch (_outputType)
                    {
                        case EOutputList.AllObject:
                            EditorGUILayout.ObjectField(gmo, typeof(GameObject));
                            break;
                        case EOutputList.ActiveOnly:
                            if (gmo.activeInHierarchy) {
                                EditorGUILayout.ObjectField(gmo, typeof(GameObject));
                            }
                            break;
                        case EOutputList.NotActiveOnly:
                            if (!gmo.activeInHierarchy)
                            {
                                EditorGUILayout.ObjectField(gmo, typeof(GameObject));
                            }
                            break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// ClearInformation
    /// </summary>
    private void Clear() {
        _activeObjectNum = 0;
        _currentSelectType = null;
        _hasComponentGameObjectList = null;
        if (_typeCntList != null)
        {
            _typeCntList.Clear();
        }
    }




    /// <summary>
    /// get All RootObject
    /// </summary>
    /// <returns></returns>
    private static List<GameObject> GetAllRootGameObjects()
    {
#if false
        // If the root object is nonactive, this program can't find... 
        // so Comment out
        List<GameObject> parentObj = new List<GameObject>();
        var allObjects = UnityEngine.Object.FindObjectsOfType<Transform>();
        _activeObjectNum = allObjects.Length;
        _snapObjectNum = 0;
        foreach (var obj in allObjects)
        {
            if (obj.parent == null)
            {
                var child = obj.GetComponentsInChildren<Transform>(true);
                if (child != null)
                {
                    _snapObjectNum += child.Length; ;
                }
                parentObj.Add(obj.gameObject);
            }
            if (obj.gameObject.activeInHierarchy)
            {
            }
        }
        return parentObj;
#else
        // you should save scene
        List<GameObject> list = new List<GameObject>();

        GameObject[] objs = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objs.Length; i++)
        {
            string path = AssetDatabase.GetAssetOrScenePath(objs[i]);
            if (path.EndsWith(".unity") && objs[i].transform.parent == null)
            {
                list.Add(objs[i]);
            }
        }
        return list;
#endif
    }

    /// <summary>
    /// Get All Objects which has "t"type component.
    /// </summary>
    /// <param name="rootGmoList"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    private List<GameObject> GetHaveComponentObjects(List<GameObject>rootGmoList, Type t )
    {
        List<GameObject> gmoList = new List<GameObject>();
        foreach (var gmo in rootGmoList)
        {
            Component[] all = SearchComponent(gmo, _searchMode);
            foreach (var obj in all)
            {
                if(obj != null && obj.GetType() == t ){gmoList.Add(obj.gameObject);}
            }
        }
         return gmoList;
    }

    /// <summary>
    /// Create Dictionary for all Objects
    /// </summary>
    /// <param name="allObjects">all objects</param>
    private void CreateTypeDictionary(List<GameObject> rootGmolist)
    {
        var _typeDict = new Dictionary<Type, ComponentCount>();
        _searchComponentFilter = _searchComponentFilter.Trim();

        foreach (var gmo in rootGmolist)
        {
            if (gmo == null) { continue; }
            Component[] all = SearchComponent(gmo, _searchMode);
            foreach (var obj in all)
            {
                if (obj == null) { continue; }
                Type t = obj.GetType();
                if (t == typeof(Transform)) { continue; }
                if (!string.IsNullOrEmpty(_searchComponentFilter) && !t.FullName.Contains(_searchComponentFilter) )
                {
                    continue;
                }
                if (!_typeDict.ContainsKey(t))
                {
                    _typeDict.Add(t, new ComponentCount(t));
                }
                _typeDict[t].allCnt += 1;
                if (obj.transform.gameObject.activeInHierarchy)
                {
                    _typeDict[t].activeCnt += 1;
                }
            }
        }
        _typeCntList.Clear();
        foreach (var t in _typeDict)
        {
            _typeCntList.Add(new ComponentCount(t.Value));
        }
        this.SortComponents();
    }

    private Component[] SearchComponent(GameObject gmo, ESearchMode mode)
    {
        Component[] all = null;
        if (mode == ESearchMode.ComponentAll)
        {
            all = gmo.GetComponentsInChildren<Component>(true);
        }
        else
        {
            all = gmo.GetComponentsInChildren<MonoBehaviour>(true);
        }
        return all;
    }


    private void SortComponents(){
        switch (_sortType) {
            case ESortType.SortByActive:
                _typeCntList.Sort(delegate(ComponentCount a, ComponentCount b) { return b.activeCnt - a.activeCnt; });
                break;
            case ESortType.SortByAll:
                _typeCntList.Sort(delegate(ComponentCount a, ComponentCount b) { return b.allCnt - a.allCnt; });
                break;
            case ESortType.SortByName:
                _typeCntList.Sort(delegate(ComponentCount a, ComponentCount b) { return string.Compare( a.typeStr , b.typeStr); });
                break;
        }
    }
}
