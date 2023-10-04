using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;

/// <summary>On the Subject of <see cref="SensoryDeprivation"/>.</summary>
public sealed class SensoryDeprivation : MonoBehaviour
{
    [SerializeField]
    KMNeedyModule _needy;

    static int s_lastModuleId;
    int _moduleId;

    MonochromeEffect _effect;
    [SerializeField]
    Shader _effectShader;

    //This should not be per bomb as the effect applies equally to every bomb.
    static readonly Queue<ModuleData> _handlerQueue = new Queue<ModuleData>();

    bool _active, _begun;


    private static SensoryDeprivation FindNext()
    {
        while (_handlerQueue.Count > 0 && !_handlerQueue.Peek().Available)
            _handlerQueue.Dequeue();
        if (_handlerQueue.Count == 0)
            return null;
        if (!_handlerQueue.Peek().Active)
        {
            _handlerQueue.Peek().Module.Begin();
            _handlerQueue.Peek().Active = true;
        }
        return _handlerQueue.Peek().Module;
    }

    static bool AddToQueue(SensoryDeprivation module)
    {
        _handlerQueue.Enqueue(new ModuleData() { Module = module, Available = true });
        return FindNext() == module;
    }

    static void RemoveFromQueue(SensoryDeprivation module)
    {
        foreach (var data in _handlerQueue.Where(d => d.Module._moduleId == module._moduleId))
            data.Available = false;
        FindNext();
    }

    void Log(string message, params object[] args)
    {
        var log = string.Format(message, args);
        Debug.LogFormat("[{0} #{1}] {2}", _needy.ModuleDisplayName, _moduleId, log);
    }

    void Start()
    {
        _moduleId = ++s_lastModuleId;
        _needy.OnActivate += NewModule;
        _needy.OnNeedyDeactivation += RemoveModule;
    }

    private void NewModule()
    {
        _needy.OnNeedyActivation += Pass;
        if (!AddToQueue(this))
            Log("This module is waiting to begin.");
    }

    private void Pass()
    {
        Log("Nothing happened... strange.");
        _needy.HandlePass();
    }

    private void RemoveModule()
    {
        RemoveFromQueue(this);
        if (_effect != null)
        {
            StopFilter();
            _effect = null;
        }
    }

    private void Begin()
    {
        Log("This module is now working.");
        _begun = true;
        _needy.OnNeedyActivation -= Pass;
        _effect = Camera.main.GetComponent<MonochromeEffect>();
        if (_effect == null)
        {
            _effect = Camera.main.gameObject.AddComponent<MonochromeEffect>();
            _effect.Shader = _effectShader;
        }
        _needy.OnNeedyActivation += StartFilter;
        _needy.OnTimerExpired += StopFilter;
    }

    void StartFilter()
    {
        Log("Your senses have been dulled.");
        _active = true;
        if (_effect != null)
        {
            _effect.RunEffect = true;
            AudioListener.pause = true;
        }
    }

    void StopFilter()
    {
        Log("Your senses are back to normal.");
        _active = false;
        if (_effect != null)
        {
            _effect.RunEffect = false;
            AudioListener.pause = false;
        }
        _needy.HandlePass();
    }

    void OnDestroy()
    {
        RemoveModule();
    }

    readonly string TwitchHelpMessage = "This module does not accept commands.";

    IEnumerator ProcessTwitchCommand(string _)
    {
        yield return "sendtochaterror This module does not accept commands.";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        RemoveFromQueue(this);
        if (_effect != null)
        {
            _effect.RunEffect = false;
            AudioListener.pause = false;
        }
        if (_active)
            _needy.HandlePass();
        if (_begun)
        {
            _needy.OnNeedyActivation -= StartFilter;
            _needy.OnNeedyActivation += Pass;
        }

        yield break;
    }

    private sealed class ModuleData
    {
        public SensoryDeprivation Module;
        public bool Available, Active;
    }
}

public sealed class MonochromeEffect : MonoBehaviour
{
    static Material EffectMaterial { get; set; }

    public bool RunEffect { set; private get; } = false;
    public Shader Shader { set { EffectMaterial = new Material(value); } }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (RunEffect && EffectMaterial != null)
            Graphics.Blit(src, dest, EffectMaterial);
        else
            Graphics.Blit(src, dest);
    }
}