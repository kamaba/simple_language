//****************************************************************************
//  File:      NicknameManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/18 12:00:00
//  Description: compile-time nickname/alias registration and resolution
//****************************************************************************

using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 编译时 Nickname 管理器。
    /// 当 @Nickname("alias") 标注在类或成员上时，注册别名，
    /// 使得通过别名同样可以查找到对应的类型或成员。
    /// </summary>
    public class NicknameManager
    {
        public static NicknameManager s_Instance;
        public static NicknameManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new NicknameManager();
                return s_Instance;
            }
        }

        // 别名 -> 宿主对象 映射
        private Dictionary<string, MetaBase> m_NicknameToMetaBaseDict = new Dictionary<string, MetaBase>();
        // 类的别名 -> MetaClass 映射（方便快速查找）
        private Dictionary<string, MetaClass> m_NicknameToMetaClassDict = new Dictionary<string, MetaClass>();
        // 类内成员别名：classAllName -> (nickname -> memberName)
        private Dictionary<string, Dictionary<string, string>> m_ClassMemberNicknameDict
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>注册别名</summary>
        public void RegisterNickname(MetaBase owner, string nickname)
        {
            if (owner == null || string.IsNullOrEmpty(nickname)) return;

            // 检查重复
            if (m_NicknameToMetaBaseDict.TryGetValue(nickname, out var existing) && existing == owner)
                return;

            m_NicknameToMetaBaseDict[nickname] = owner;

            if (owner is MetaClass mc)
            {
                m_NicknameToMetaClassDict[nickname] = mc;
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    $"Nickname registered: '{nickname}' -> class '{mc.allName}'");
            }
            else if (owner is MetaMemberVariable mmv)
            {
                // 成员变量别名：记录在所属类的映射中
                var classAllName = mmv.ownerMetaClass?.allName ?? "";
                if (!string.IsNullOrEmpty(classAllName))
                {
                    if (!m_ClassMemberNicknameDict.TryGetValue(classAllName, out var memberMap))
                    {
                        memberMap = new Dictionary<string, string>();
                        m_ClassMemberNicknameDict[classAllName] = memberMap;
                    }
                    memberMap[nickname] = mmv.name;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage,
                        $"Nickname registered: '{nickname}' -> variable '{mmv.name}' in '{classAllName}'");
                }
            }
            else if (owner is MetaMemberFunction mmf)
            {
                var classAllName = mmf.ownerMetaClass?.allName ?? "";
                if (!string.IsNullOrEmpty(classAllName))
                {
                    if (!m_ClassMemberNicknameDict.TryGetValue(classAllName, out var memberMap))
                    {
                        memberMap = new Dictionary<string, string>();
                        m_ClassMemberNicknameDict[classAllName] = memberMap;
                    }
                    memberMap[nickname] = mmf.name;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage,
                        $"Nickname registered: '{nickname}' -> function '{mmf.name}' in '{classAllName}'");
                }
            }
        }

        /// <summary>通过别名查找 MetaClass</summary>
        public MetaClass ResolveClassByNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return null;
            m_NicknameToMetaClassDict.TryGetValue(nickname, out var mc);
            return mc;
        }

        /// <summary>通过别名查找任意 MetaBase</summary>
        public MetaBase ResolveByNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return null;
            m_NicknameToMetaBaseDict.TryGetValue(nickname, out var mb);
            return mb;
        }

        /// <summary>通过别名查找类内成员的真实名称</summary>
        public string ResolveMemberNickname(string classAllName, string nickname)
        {
            if (string.IsNullOrEmpty(classAllName) || string.IsNullOrEmpty(nickname)) return null;
            if (m_ClassMemberNicknameDict.TryGetValue(classAllName, out var memberMap))
            {
                memberMap.TryGetValue(nickname, out var realName);
                return realName;
            }
            return null;
        }

        /// <summary>检查名称是否是已注册的别名</summary>
        public bool IsNickname(string name)
        {
            return !string.IsNullOrEmpty(name) && m_NicknameToMetaBaseDict.ContainsKey(name);
        }

        /// <summary>清空所有别名注册（用于重新编译）</summary>
        public void Clear()
        {
            m_NicknameToMetaBaseDict.Clear();
            m_NicknameToMetaClassDict.Clear();
            m_ClassMemberNicknameDict.Clear();
        }
    }
}
