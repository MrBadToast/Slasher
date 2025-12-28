using Sirenix.OdinInspector;
using UnityEngine;

public class StaticMonoBehaviour<T> : SerializedMonoBehaviour
    where T : SerializedMonoBehaviour
{
    #region description
    // =====================================
    //  !!매우 중요!!
    //  이 클래스를 상속받으면 MonoBehaviour를 사용하는 싱글턴 클래스가 됩니다.
    //  이 클래스는 DontDestroyOnLoad에 포함되지 않습니다.
    //  반드시 이 함수를 상속받고 Awake()를 사용할 때 아래 Awake()를 오버라이딩하고 base.Awake()를 사용하세요!
    //
    //  예시)
    //  protected override void Awake()
    //  {
    //      base.Awake();
    //
    //      input = new MainPlayerInputActions();
    //  }
    //
    // =====================================
    #endregion

    static private T instance;
    /// <summary>
    /// 싱글턴 인스턴스를 받아옵니다.
    /// </summary>
    static public T Instance { get { return instance; } }
    /// <summary>
    /// 현재 인스턴스가 정상적으로 존재하는지 확인합니다.
    /// </summary>
    static public bool IsInstanceValid { get { return instance != null; } }

    protected virtual void Awake()
    {
        if(instance == null) { instance = this as T; }
        else { Debug.LogWarning(typeof(T).Name + " : Duplicated SingletonObject, "+ gameObject.name + " : This Object Will be Destroyed."); Destroy(gameObject); }
    }

}
