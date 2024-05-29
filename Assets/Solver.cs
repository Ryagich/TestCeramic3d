using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField] private TextAsset _modelJson;
    [SerializeField] private TextAsset _spaceJson;

    [SerializeField] private GameObject _modelPref;
    [SerializeField] private GameObject _spacePref;

    [SerializeField] private List<Matrix4x4> _model = new();
    [SerializeField] private List<Matrix4x4> _space = new();
    [SerializeField] private List<Matrix4x4> _validOffsets = new();


    [SerializeField] private float _threshold = 0.001f;
    [SerializeField] private GameObject _modelParent;
    [SerializeField] private GameObject _spaceParent;

    private void Start()
    {
        StartCoroutine(AnimateOffsets());
    }

    [Button]
    private void LoadData()
    {
        _model = JsonConvert.DeserializeObject<List<Matrix4x4>>(_modelJson.text);
        _space = JsonConvert.DeserializeObject<List<Matrix4x4>>(_spaceJson.text);
    }

    [Button]
    private void Solve()
    {
        _validOffsets.Clear();
        var m = _model.First();
        foreach (var s in _space)
        {
            if (m.determinant != 0)
            {
                var offset = s * m.inverse;
                if (IsValidOffset(offset))
                    _validOffsets.Add(offset);
            }
        }
    }

    private bool IsValidOffset(Matrix4x4 offset)
    {
        return _model.All(m => _space.Any(s => IsEqual(offset * m, s, _threshold)));
    }

    [Button]
    private void Export()
    {
        var validOffsetsJson = JsonConvert.SerializeObject(_validOffsets, new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new WhiteListPropertiesResolver(GetMatrixPropNames())
        });
        var path = EditorUtility.SaveFilePanel("Export offsets", null, "output", "json");
        if (!string.IsNullOrWhiteSpace(path))
            File.WriteAllText(path, validOffsetsJson);
    }

    private static IEnumerable<string> GetMatrixPropNames()
    {
        for (var x = 0; x < 4; x++)
        {
            for (var y = 0; y < 4; y++)
            {
                yield return $"m{x}{y}";
            }
        }
    }

    [Button]
    private void SpawnGameObjects()
    {
        if (_modelPref)
        {
            DestroyImmediate(_modelParent);
        }
        _modelParent = new GameObject("Model");
        foreach (var m in _model)
        {
            Spawn(m, _modelPref, _modelParent.transform);
        }

        if (_spaceParent)
        {
            DestroyImmediate(_spaceParent);
        }
        _spaceParent = new GameObject("Space");
        foreach (var s in _space)
        {
            Spawn(s, _spacePref, _spaceParent.transform);
        }
    }

    private IEnumerator AnimateOffsets()
    {
        if (_validOffsets.Count <= 0 || !_modelParent)
        {
            Debug.LogWarning("No offsets calculated or model parent created!");
            yield break;
        }
        const int animationFrames = 120;
        var currentOffset = 0;
        while (true)
        {
            var from = _validOffsets[currentOffset % _validOffsets.Count];
            var to = _validOffsets[(currentOffset + 1) % _validOffsets.Count];
            for (var i = 0; i <= animationFrames; i++)
            {
                var t = i / (float)animationFrames;
                _modelParent.transform.position = Vector3.Lerp(from.GetPosition(), to.GetPosition(), t);
                _modelParent.transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, t);
                _modelParent.transform.localScale = Vector3.Lerp(from.lossyScale, to.lossyScale, t);
                yield return null;
            }
            currentOffset++;
            yield return new WaitForSeconds(3);
        }
    }

    private static void Spawn(Matrix4x4 matrix, GameObject pref, Transform parent)
    {
        var newObj = Instantiate(pref).transform;
        newObj.position = matrix.GetPosition();
        newObj.rotation = matrix.rotation;
        newObj.localScale = matrix.lossyScale;
        newObj.SetParent(parent, true);
    }

    private static bool IsEqual(Matrix4x4 a, Matrix4x4 b, float threshold)
    {
        var delta = 0f;
        for (var i = 0; i < 4; i++)
            delta += (a.GetRow(i) - b.GetRow(i)).sqrMagnitude;
        return delta < threshold;
    }
}