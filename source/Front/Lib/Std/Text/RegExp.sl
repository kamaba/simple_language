#正则表达式引擎（纯 .sl 实现，回溯式，仿 C# System.Text.RegularExpressions / Python re）。
#字符串按 UTF-8 字节序列处理：位置均为字节下标；readChar 解码完整 UTF-8 序列，
#因此 "." 与字面多字节字符均按"一个字符"消费，不会拆散中文等多字节文本。
#典型用法：
#    RegExp re = RegExp( "(\w+)@(\w+)\.com" )
#    RegExpMatch m = re.search( "mail: bob@exa.com" )
#    if m != null { print( m.group( 1 ) ) }
#支持：字面字符（含非 ASCII）、. 、[...] / [^...]（区间、\d\w\s 及否定）、
#量词 * + ? {n} {n,} {n,m} 与懒惰后缀 ?、分组 (...)、非捕获 (?:...)、
#选择 |、锚点 ^ $ \A \z \b \B、前瞻 (?=) (?!)、反向引用 \1-\9、
#转义 \n \t \r \\ \. 等；不支持的语法在编译期抛 RegExpError。
namespace Text
{
    #正则错误码（构造/编译期抛出）
    public enum RegExpError extends Error
    {
        # 1 模式非法（通用：空模式、无法识别的构造等）
        PatternSyntax = { code = 1 }
        # 2 量词非法（{n,m} 中 n>m、重复次数过大等）
        InvalidRepeat = { code = 2 }
        # 3 非法转义（\ 后跟无意义字符且非字面）
        InvalidEscape = { code = 3 }
        # 4 字符类内部非法（缺 ] 等）
        InvalidCharClass = { code = 4 }
        # 5 反向引用组号超界（\1-\9 中无对应组）
        InvalidGroupRef = { code = 5 }
        # 6 括号不配对（缺 ) 或多 ) ）
        UnbalancedParen = { code = 6 }
        # 7 量词前无可重复单元
        InvalidRepeatTarget = { code = 7 }
    }

    # ===============================================================
    # RegExpCharSet - 字符集合（[...] 与 \d\w\s 的公共载体）
    # ASCII 部分（码点 0..127）用 128 槽 0/1 表；非 ASCII 部分用区间表；
    # _negate 为真时结果取反（[^...] / \D\W\S）。
    # ===============================================================
    public class RegExpCharSet
    {
        public Array<Int32> _ascii = null      # 128 槽 0/1
        public List<Int32> _loList = null      # 非 ASCII 区间下限（码点）
        public List<Int32> _hiList = null      # 非 ASCII 区间上限
        public bool _negate = false

        _init_()
        {
            this._ascii = Array<Int32>( 128 )
            this._loList = List<Int32>()
            this._hiList = List<Int32>()
            this._negate = false
            Int32 i = 0
            while i < 128
            {
                this._ascii[i] = 0
                i = i + 1
            }
        }

        #加入闭区间 [lo, hi]（码点）
        public void addRange( Int32 lo, Int32 hi )
        {
            if hi < lo
            {
                ret
            }
            if hi < 128
            {
                Int32 i = lo
                while i <= hi
                {
                    this._ascii[i] = 1
                    i = i + 1
                }
                ret
            }
            if lo < 128
            {
                Int32 j = lo
                while j < 128
                {
                    this._ascii[j] = 1
                    j = j + 1
                }
                this._loList.add( 128 )
                this._hiList.add( hi )
                ret
            }
            this._loList.add( lo )
            this._hiList.add( hi )
        }

        #加入单个码点
        public void addChar( Int32 cp )
        {
            this.addRange( cp, cp )
        }

        #加入 \d 集合（数字）
        public void addDigit()
        {
            this.addRange( 48, 57 )
        }

        #加入 \w 集合（数字 + 字母 + 下划线 + 全部非 ASCII，与 Python3/C# 的 Unicode 语义一致）
        public void addWord()
        {
            this.addRange( 48, 57 )
            this.addRange( 65, 90 )
            this.addRange( 95, 95 )
            this.addRange( 97, 122 )
            this.addRange( 128, 1114111 )
        }

        #加入 \s 集合（空白）
        public void addSpace()
        {
            this.addRange( 9, 13 )
            this.addRange( 32, 32 )
        }

        #忽略大小写模式：把已加入集合的 ASCII 字母补上另一大小写
        public void applyIgnoreCase()
        {
            Int32 c = 97
            while c <= 122
            {
                if this._ascii[c] == 1 && this._ascii[c - 32] == 0
                {
                    this._ascii[c - 32] = 1
                }
                c = c + 1
            }
            c = 65
            while c <= 90
            {
                if this._ascii[c] == 1 && this._ascii[c + 32] == 0
                {
                    this._ascii[c + 32] = 1
                }
                c = c + 1
            }
        }

        #码点是否属于本集合（考虑 _negate）
        public bool contains( Int32 cp )
        {
            bool hit = false
            if cp >= 0 && cp < 128
            {
                hit = this._ascii[cp] == 1
            }
            else
            {
                Int32 n = this._loList.length
                Int32 i = 0
                while i < n
                {
                    Int32 lo = this._loList[i]
                    Int32 hi = this._hiList[i]
                    if cp >= lo && cp <= hi
                    {
                        hit = true
                        break
                    }
                    i = i + 1
                }
            }
            if this._negate
            {
                ret !hit
            }
            ret hit
        }
    }

    # ===============================================================
    # RegExpInst - 编译后的指令（字段含义随 op 变化，见 RegExp 类操作码注释）
    # ===============================================================
    public class RegExpInst
    {
        public Int32 op = 0    # 操作码
        public Int32 ch = 0    # CHAR/CHAR_REP: 折叠后码点
        public Int32 x = 0     # CHAR: 字节长 | CLASS: charset 下标 | SPLIT: 分支 x | JMP: 目标 | SAVE: 槽位 | BACKREF: 组号 | LAHEAD: 子链起点
        public Int32 y = 0     # SPLIT: 分支 y | REP: 下限 min | LAHEAD: 子链终点
        public Int32 z = 0     # REP: 上限 max（-1 = 无限）
        public Int32 w = 0     # REP: 1 = 懒惰
        public Int32 v = 0     # GUARD: 槽位号

        _init_( Int32 opCode )
        {
            this.op = opCode
            this.ch = 0
            this.x = 0
            this.y = 0
            this.z = 0
            this.w = 0
            this.v = 0
        }
    }

    # ===============================================================
    # RegExpMatch - 一次成功匹配的结果
    # index/end 为字节位置；group(0) 为整体匹配；组未参与匹配时 group 返回空串。
    # ===============================================================
    public class RegExpMatch
    {
        public string _text = null
        public Int32 index = 0
        public Int32 end = 0
        public Array<Int32> _slots = null
        public Int32 _groupCount = 0

        _init_( string text, Int32 startIndex, Int32 endIndex, Array<Int32> slots, Int32 groupCount )
        {
            this._text = text
            this.index = startIndex
            this.end = endIndex
            this._slots = slots
            this._groupCount = groupCount
        }

        #匹配整体文本
        public string value()
        {
            String t = this._text
            Int32 vs = this.index
            Int32 ve = this.end
            ret SystemStringRange( t, vs, ve )
        }

        #组 g 文本（0 = 整体；未参与匹配返回空串）
        public string group( Int32 g )
        {
            if g < 0 || g > this._groupCount
            {
                ret ""
            }
            Int32 s = this._slots[g * 2]
            Int32 e = this._slots[g * 2 + 1]
            if s < 0 || e < 0 || e < s
            {
                ret ""
            }
            String gt = this._text
            ret SystemStringRange( gt, s, e )
        }

        #组 g 是否参与了本次匹配
        public bool groupOk( Int32 g )
        {
            if g < 0 || g > this._groupCount
            {
                ret false
            }
            Int32 s = this._slots[g * 2]
            Int32 e = this._slots[g * 2 + 1]
            ret s >= 0 && e >= 0 && e >= s
        }

        #匹配长度（字节数）
        public get Int32 length()
        {
            ret this.end - this.index
        }
    }

    # ===============================================================
    # RegExpState - 单次匹配的执行状态（每次匹配新建，天然并发安全）
    # ===============================================================
    public class RegExpState
    {
        public string _text = null
        public Int32 _len = 0
        public Array<Int32> _slots = null     # 捕获槽：组 g 起点=_slots[2g]、终点=_slots[2g+1]
        public Array<Int32> _guards = null    # 防空转槽（-1 = 未用）
        public Int32 _cp = 0                  # readChar 解出的码点
        public Int32 _clen = 0                # readChar 解出的字节长

        _init_( string text, Int32 slotCount, Int32 guardCount )
        {
            this._text = text
            this._len = SystemStringLength( text )
            this._slots = Array<Int32>( slotCount )
            if guardCount < 1
            {
                guardCount = 1
            }
            this._guards = Array<Int32>( guardCount )
            this._cp = 0
            this._clen = 0
            Int32 i = 0
            while i < slotCount
            {
                this._slots[i] = -1
                i = i + 1
            }
            i = 0
            while i < this._guards.length
            {
                this._guards[i] = -1
                i = i + 1
            }
        }

        #读第 i 字节；越界返回 -1
        public Int32 byteAt( Int32 i )
        {
            if i < 0 || i >= this._len
            {
                ret -1
            }
            String bt = this._text
            ret SystemStringCharCodeAt( bt, i )
        }

        #从字节位置 i 解码一个完整 UTF-8 序列，结果写入 _cp/_clen；i 越界返回 false
        public bool readChar( Int32 i )
        {
            if i < 0 || i >= this._len
            {
                ret false
            }
            String t = this._text
            Int32 b = SystemStringCharCodeAt( t, i )
            Int32 n = 1
            if b >= 0xF0
            {
                n = 4
            }
            elif b >= 0xE0
            {
                n = 3
            }
            elif b >= 0xC0
            {
                n = 2
            }
            if i + n > this._len
            {
                n = 1
            }
            this._clen = n
            if n == 1
            {
                this._cp = b
            }
            elif n == 2
            {
                this._cp = ( ( b & 0x1F ) << 6 ) | ( SystemStringCharCodeAt( t, i + 1 ) & 0x3F )
            }
            elif n == 3
            {
                this._cp = ( ( b & 0x0F ) << 12 ) | ( ( SystemStringCharCodeAt( t, i + 1 ) & 0x3F ) << 6 ) | ( SystemStringCharCodeAt( t, i + 2 ) & 0x3F )
            }
            else
            {
                this._cp = ( ( b & 0x07 ) << 18 ) | ( ( SystemStringCharCodeAt( t, i + 1 ) & 0x3F ) << 12 ) | ( ( SystemStringCharCodeAt( t, i + 2 ) & 0x3F ) << 6 ) | ( SystemStringCharCodeAt( t, i + 3 ) & 0x3F )
            }
            ret true
        }

        #回退：返回 < sp 的最近一个完整 UTF-8 序列起点（贪婪 REP 的逐字符回退用）
        public Int32 backChar( Int32 sp )
        {
            Int32 p = sp - 1
            if p < 0
            {
                ret 0
            }
            while p > 0
            {
                String bt = this._text
                Int32 b = SystemStringCharCodeAt( bt, p )
                if b < 0x80 || b >= 0xC0
                {
                    break
                }
                p = p - 1
            }
            ret p
        }

        public Array<Int32> copySlots()
        {
            Int32 n = this._slots.length
            Array<Int32> dst = Array<Int32>( n )
            Int32 i = 0
            while i < n
            {
                dst[i] = this._slots[i]
                i = i + 1
            }
            ret dst
        }

        public void restoreSlots( Array<Int32> src )
        {
            Int32 n = this._slots.length
            Int32 i = 0
            while i < n
            {
                this._slots[i] = src[i]
                i = i + 1
            }
        }

        public Array<Int32> copyGuards()
        {
            Int32 n = this._guards.length
            Array<Int32> dst = Array<Int32>( n )
            Int32 i = 0
            while i < n
            {
                dst[i] = this._guards[i]
                i = i + 1
            }
            ret dst
        }

        public void restoreGuards( Array<Int32> src )
        {
            Int32 n = this._guards.length
            Int32 i = 0
            while i < n
            {
                this._guards[i] = src[i]
                i = i + 1
            }
        }
    }

    # ===============================================================
    # RegExp - 正则表达式主类
    # 模式编译为指令链（List<RegExpInst>，x/y/z/w/v 字段含义见各操作码），
    # 匹配时经 _run 递归回溯执行；每次匹配新建 RegExpState，实例可复用。
    # ===============================================================
    public class RegExp
    {
        # ---- 操作码 ----
        public const static Int32 OP_NOP = 0            # 空操作（占位/已 NOP 化）
        public const static Int32 OP_CHAR = 1           # ch=码点, x=字节长
        public const static Int32 OP_CLASS = 2          # x=charset 下标
        public const static Int32 OP_DOT = 3            # 任意一个字符（不含 \n）
        public const static Int32 OP_SPLIT = 4          # x/y=两分支（先试 x）
        public const static Int32 OP_JMP = 5            # x=目标
        public const static Int32 OP_SAVE = 6           # x=槽位，记录当前 sp
        public const static Int32 OP_MATCH = 7          # 整体成功
        public const static Int32 OP_BOL = 9            # ^（multiline 受 _multiline 影响）
        public const static Int32 OP_EOL = 10           # $（\n 前或串尾）
        public const static Int32 OP_WORDB = 11         # \b 词边界
        public const static Int32 OP_NWORDB = 12        # \B 非词边界
        public const static Int32 OP_BACKREF = 13       # x=组号
        public const static Int32 OP_CHAR_REP = 14      # ch=码点 x=字节长 y=min z=max w=lazy
        public const static Int32 OP_CLASS_REP = 15     # x=charset 下标 y/z/w 同上
        public const static Int32 OP_DOT_REP = 16       # y/z/w 同上
        public const static Int32 OP_GUARD = 17         # v=槽位，防复杂量词空转死循环
        public const static Int32 OP_LAHEAD_POS = 18    # x/y=子链区间，正前瞻
        public const static Int32 OP_LAHEAD_NEG = 19    # x/y=子链区间，负前瞻

        # ---- 实例状态 ----
        string _pattern = null
        bool _ignoreCase = false
        bool _multiline = false
        List<RegExpInst> _prog = null
        List<RegExpCharSet> _charsets = null
        Int32 _groupCount = 0
        Int32 _guardCount = 0
        Int32 _patPos = 0
        Int32 _patLen = 0
        Array<Int32> _flatProgCache = null   # 展平指令链缓存（C 下沉用）
        Array<Int32> _flatCsCache = null     # 展平字符集段表缓存（C 下沉用）

        _init_( string pattern ) throws
        {
            this._commonInit( pattern, false, false )
        }

        _init_( string pattern, bool ignoreCase ) throws
        {
            this._commonInit( pattern, ignoreCase, false )
        }

        _init_( string pattern, bool ignoreCase, bool multiline ) throws
        {
            this._commonInit( pattern, ignoreCase, multiline )
        }

        void _commonInit( string pattern, bool ignoreCase, bool multiline ) throws
        {
            if pattern == null
            {
                throw RegExpError.PatternSyntax
            }
            this._pattern = pattern
            this._ignoreCase = ignoreCase
            this._multiline = multiline
            this._prog = List<RegExpInst>()
            this._charsets = List<RegExpCharSet>()
            this._groupCount = 0
            this._guardCount = 0
            this._patPos = 0
            this._patLen = SystemStringLength( pattern )
            this._compile()
        }

        # ---- 编译 ----
        #主链：SAVE(0) <alternation> SAVE(1) MATCH
        void _compile() throws
        {
            RegExpInst s0 = this._emit( RegExp.OP_SAVE )
            s0.x = 0
            this._compileAlt()
            RegExpInst s1 = this._emit( RegExp.OP_SAVE )
            s1.x = 1
            this._emit( RegExp.OP_MATCH )
            if this._patPos < this._patLen
            {
                #compileAlt 在非 '|' 位置停下只可能是多余的 ')'
                throw RegExpError.UnbalancedParen
            }
        }

        RegExpInst _emit( Int32 op )
        {
            RegExpInst inst = RegExpInst( op )
            this._prog.add( inst )
            ret inst
        }

        Int32 _here()
        {
            ret this._prog.length
        }

        #读模式第 i 字节；越界返回 -1
        Int32 _patAt( Int32 i )
        {
            if i < 0 || i >= this._patLen
            {
                ret -1
            }
            String pt = this._pattern
            ret SystemStringCharCodeAt( pt, i )
        }

        #分配一个防空转槽位
        Int32 _newGuard()
        {
            this._guardCount = this._guardCount + 1
            ret this._guardCount - 1
        }

        #编译 alternation：分支间以 SPLIT 串联，分支尾 JMP 汇合
        #布局（a|b|c）：SPLIT(1,3) CHAR-a JMP(e) SPLIT(4,6) CHAR-b JMP(e) CHAR-c JMP(e)
        void _compileAlt() throws
        {
            Int32 split0 = this._here()
            this._emit( RegExp.OP_SPLIT )
            Int32 firstStart = this._here()
            this._compileSeq()
            Int32 prevSplit = split0
            Int32 firstJmp = this._here()
            this._emit( RegExp.OP_JMP )
            List<Int32> jmps = List<Int32>()
            jmps.add( firstJmp )
            Int32 ap = this._patPos
            while this._patAt( ap ) == 124
            {
                this._patPos = this._patPos + 1
                if prevSplit == split0
                {
                    #首个 '|'：split0.x → 第一分支，split0.y → 第二分支的新 SPLIT 占位
                    RegExpInst p0 = this._prog[split0]
                    p0.x = firstStart
                    p0.y = this._here()
                }
                else
                {
                    RegExpInst pp = this._prog[prevSplit]
                    pp.y = this._here()
                }
                RegExpInst sp = this._emit( RegExp.OP_SPLIT )
                sp.x = this._here()
                prevSplit = this._here() - 1
                this._compileSeq()
                Int32 j = this._here()
                this._emit( RegExp.OP_JMP )
                jmps.add( j )
                ap = this._patPos
            }
            Int32 endPos = this._here()
            if prevSplit == split0
            {
                #无 '|'：单顺序序列，占位 SPLIT/JMP NOP 化
                RegExpInst p1 = this._prog[split0]
                p1.op = RegExp.OP_NOP
                RegExpInst p2 = this._prog[firstJmp]
                p2.op = RegExp.OP_NOP
                ret
            }
            #末分支前的 SPLIT NOP 化：若设 y=endPos 会产生虚假"空匹配"路径
            #（如 (a|b)+ 末轮迭代走空分支、SAVE 覆盖组槽致 group(1) 变空串）
            #前驱 SPLIT.y 落到 NOP 后直入末分支，与 L552 布局注释一致（末分支无 SPLIT）
            RegExpInst pe = this._prog[prevSplit]
            pe.op = RegExp.OP_NOP
            Int32 i = 0
            while i < jmps.length
            {
                Int32 jp = jmps[i]
                RegExpInst pj = this._prog[jp]
                pj.x = endPos
                i = i + 1
            }
        }

        #编译顺序序列：直到 '|'、')' 或串尾
        void _compileSeq() throws
        {
            bool first = true
            while this._patPos < this._patLen
            {
                String sp = this._pattern
                Int32 sq = this._patPos
                Int32 c = SystemStringCharCodeAt( sp, sq )
                if c == 124 || c == 41
                {
                    ret
                }
                if first && ( c == 42 || c == 43 || c == 63 )
                {
                    #序列首位出现 * + ?：无可重复单元（'{' 首位按字面处理）
                    throw RegExpError.InvalidRepeatTarget
                }
                first = false
                this._compileAtom()
            }
        }

        # ---- 原子编译分派 ----
        void _compileAtom() throws
        {
            Int32 ap = this._patPos
            Int32 c = this._patAt( ap )
            if c == 40
            {
                # '(' 分组/非捕获/前瞻（复杂原子）
                this._compileGroup()
            }
            elif c == 91
            {
                # '[' 字符类（简单原子）
                this._compileCharClassAtom()
            }
            elif c == 46
            {
                # '.'
                this._patPos = this._patPos + 1
                Int32 atomStart = this._here()
                this._emit( RegExp.OP_DOT )
                this._applySimpleRepeat( RegExp.OP_DOT_REP, atomStart )
            }
            elif c == 94
            {
                # '^'（w=0）
                this._patPos = this._patPos + 1
                RegExpInst bi = this._emit( RegExp.OP_BOL )
                bi.w = 0
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
            }
            elif c == 36
            {
                # '$'（w=0）
                this._patPos = this._patPos + 1
                RegExpInst ei = this._emit( RegExp.OP_EOL )
                ei.w = 0
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
            }
            elif c == 92
            {
                # '\' 转义
                this._compileEscapeAtom()
            }
            else
            {
                #字面字符：解码完整 UTF-8 序列
                Int32 dp = this._patPos
                Int32 cp = this._decodePatChar( dp )
                Int32 n = this._decLen
                this._patPos = this._patPos + n
                Int32 atomStart = this._here()
                RegExpInst ci = this._emit( RegExp.OP_CHAR )
                ci.ch = this._foldCp( cp )
                ci.x = n
                this._applySimpleRepeat( RegExp.OP_CHAR_REP, atomStart )
            }
        }

        #简单原子（CHAR/CLASS/DOT）量词：原地改写为单条 *_REP 指令
        #（每次迭代至少消费 1 字节，无需 SPLIT/GUARD）
        void _applySimpleRepeat( Int32 repOp, Int32 atomStart ) throws
        {
            if !this._parseQuantifier()
            {
                ret
            }
            RegExpInst inst = this._prog[atomStart]
            inst.op = repOp
            inst.y = this._qMin
            inst.z = this._qMax
            inst.w = this._qLazy ? 1 : 0
        }

        # ---- 量词解析（结果写入 _qMin/_qMax/_qLazy）----
        Int32 _qMin = 0
        Int32 _qMax = 0
        bool _qLazy = false

        #当前位置是量词则消费并返回 true；'{' 试解失败不消费（当字面）
        bool _parseQuantifier() throws
        {
            this._qMin = 0
            this._qMax = 0
            this._qLazy = false
            Int32 qp = this._patPos
            Int32 c = this._patAt( qp )
            if c == 42
            {
                this._patPos = this._patPos + 1
                this._qMin = 0
                this._qMax = -1
            }
            elif c == 43
            {
                this._patPos = this._patPos + 1
                this._qMin = 1
                this._qMax = -1
            }
            elif c == 63
            {
                this._patPos = this._patPos + 1
                this._qMin = 0
                this._qMax = 1
            }
            elif c == 123
            {
                Int32 save = this._patPos
                this._patPos = this._patPos + 1
                Int32 mn = this._parseNumber()
                if mn < 0
                {
                    this._patPos = save
                    ret false
                }
                Int32 mx = mn
                Int32 q1 = this._patPos
                if this._patAt( q1 ) == 44
                {
                    this._patPos = this._patPos + 1
                    Int32 m2 = this._parseNumber()
                    if m2 < 0
                    {
                        mx = -1
                    }
                    else
                    {
                        mx = m2
                    }
                }
                Int32 q2 = this._patPos
                if this._patAt( q2 ) != 125
                {
                    this._patPos = save
                    ret false
                }
                this._patPos = this._patPos + 1
                if mx != -1 && mx < mn
                {
                    throw RegExpError.InvalidRepeat
                }
                if mn > 1000 || ( mx != -1 && mx > 1000 )
                {
                    throw RegExpError.InvalidRepeat
                }
                this._qMin = mn
                this._qMax = mx
            }
            else
            {
                ret false
            }
            Int32 q3 = this._patPos
            if this._patAt( q3 ) == 63
            {
                this._patPos = this._patPos + 1
                this._qLazy = true
            }
            ret true
        }

        #读一个十进制数；无数字返回 -1（不回退位置）
        Int32 _parseNumber()
        {
            Int32 v = 0
            Int32 n = 0
            Int32 np = this._patPos
            Int32 c = this._patAt( np )
            while c >= 48 && c <= 57
            {
                v = v * 10 + ( c - 48 )
                this._patPos = this._patPos + 1
                n = n + 1
                np = this._patPos
                c = this._patAt( np )
            }
            if n == 0
            {
                ret -1
            }
            ret v
        }

        #解码模式中 pos 起的完整 UTF-8 序列，返回码点（字节长写 _decLen）
        Int32 _decLen = 1
        Int32 _decodePatChar( Int32 pos )
        {
            Int32 b = this._patAt( pos )
            Int32 n = 1
            if b >= 0xF0
            {
                n = 4
            }
            elif b >= 0xE0
            {
                n = 3
            }
            elif b >= 0xC0
            {
                n = 2
            }
            if pos + n > this._patLen
            {
                n = 1
            }
            this._decLen = n
            if n == 1
            {
                ret b
            }
            elif n == 2
            {
                ret ( ( b & 0x1F ) << 6 ) | ( this._patAt( pos + 1 ) & 0x3F )
            }
            elif n == 3
            {
                ret ( ( b & 0x0F ) << 12 ) | ( ( this._patAt( pos + 1 ) & 0x3F ) << 6 ) | ( this._patAt( pos + 2 ) & 0x3F )
            }
            ret ( ( b & 0x07 ) << 18 ) | ( ( this._patAt( pos + 1 ) & 0x3F ) << 12 ) | ( ( this._patAt( pos + 2 ) & 0x3F ) << 6 ) | ( this._patAt( pos + 3 ) & 0x3F )
        }

        #码点折叠（忽略大小写模式下 ASCII 大小写互折）
        Int32 _foldCp( Int32 cp )
        {
            if !this._ignoreCase
            {
                ret cp
            }
            if cp >= 65 && cp <= 90
            {
                ret cp + 32
            }
            ret cp
        }

        # ---- 分组编译：'(' 由 _compileAtom 分派（未消费），本方法消费 ----
        #kind: 0=捕获组 1=非捕获组(?:) 2=正前瞻(?=) 3=负前瞻(?!)
        void _compileGroup() throws
        {
            this._patPos = this._patPos + 1
            Int32 kind = 0
            Int32 gp = this._patPos
            if this._patAt( gp ) == 63
            {
                Int32 gpn = this._patPos + 1
                Int32 nx = this._patAt( gpn )
                if nx == 58
                {
                    kind = 1
                }
                elif nx == 61
                {
                    kind = 2
                }
                elif nx == 33
                {
                    kind = 3
                }
                else
                {
                    throw RegExpError.PatternSyntax
                }
                this._patPos = this._patPos + 2
            }
            if kind >= 2
            {
                #前瞻：先占位 LAHEAD，再编子链 [subStart, subEnd)，回填 x/y
                #主流程经 LAHEAD.y 越过子链；子链只由递归执行（sp 不推进）
                Int32 laOp = RegExp.OP_LAHEAD_POS
                if kind == 3
                {
                    laOp = RegExp.OP_LAHEAD_NEG
                }
                RegExpInst la = this._emit( laOp )
                Int32 subStart = this._here()
                this._compileAlt()
                Int32 ge1 = this._patPos
                if this._patAt( ge1 ) != 41
                {
                    throw RegExpError.UnbalancedParen
                }
                this._patPos = this._patPos + 1
                la.x = subStart
                la.y = this._here()
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
                ret
            }
            #捕获/非捕获：双占位（splitPh + guardPh），量词确定后由 _applyComplexRepeat 回填
            Int32 splitPh = this._here()
            this._emit( RegExp.OP_NOP )
            Int32 guardPh = this._here()
            this._emit( RegExp.OP_NOP )
            Int32 g = 0
            if kind == 0
            {
                this._groupCount = this._groupCount + 1
                g = this._groupCount
                RegExpInst si = this._emit( RegExp.OP_SAVE )
                si.x = g * 2
            }
            this._compileAlt()
            Int32 ge2 = this._patPos
            if this._patAt( ge2 ) != 41
            {
                throw RegExpError.UnbalancedParen
            }
            this._patPos = this._patPos + 1
            if kind == 0
            {
                RegExpInst ei = this._emit( RegExp.OP_SAVE )
                ei.x = g * 2 + 1
            }
            Int32 atomStart = splitPh + 2
            Int32 atomEnd = this._here()
            if this._parseQuantifier()
            {
                this._applyComplexRepeat( splitPh, guardPh, atomStart, atomEnd )
            }
        }

        # ---- 转义原子编译：进入时 '\' 未消费 ----
        void _compileEscapeAtom() throws
        {
            this._patPos = this._patPos + 1
            Int32 ep = this._patPos
            Int32 c = this._patAt( ep )
            if c < 0
            {
                throw RegExpError.InvalidEscape
            }
            if c == 100 || c == 68 || c == 119 || c == 87 || c == 115 || c == 83
            {
                # \d \D \w \W \s \S → 字符类指令
                RegExpCharSet cs = RegExpCharSet()
                if c == 100 || c == 119 || c == 115
                {
                    if c == 100
                    {
                        cs.addDigit()
                    }
                    elif c == 119
                    {
                        cs.addWord()
                    }
                    else
                    {
                        cs.addSpace()
                    }
                }
                else
                {
                    #否定简写：先建正集合再取反
                    if c == 68
                    {
                        cs.addDigit()
                    }
                    elif c == 87
                    {
                        cs.addWord()
                    }
                    else
                    {
                        cs.addSpace()
                    }
                    cs._negate = true
                }
                if this._ignoreCase
                {
                    cs.applyIgnoreCase()
                }
                this._patPos = this._patPos + 1
                Int32 atomStart = this._here()
                RegExpInst ci = this._emit( RegExp.OP_CLASS )
                ci.x = this._charsets.length
                this._charsets.add( cs )
                this._applySimpleRepeat( RegExp.OP_CLASS_REP, atomStart )
                ret
            }
            if c == 98 || c == 66
            {
                # \b 词边界 / \B 非词边界（不可加量词）
                this._patPos = this._patPos + 1
                if c == 98
                {
                    this._emit( RegExp.OP_WORDB )
                }
                else
                {
                    this._emit( RegExp.OP_NWORDB )
                }
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
                ret
            }
            if c == 65
            {
                # \A 串首
                this._patPos = this._patPos + 1
                RegExpInst bi = this._emit( RegExp.OP_BOL )
                bi.w = 1
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
                ret
            }
            if c == 122
            {
                # \z 串尾
                this._patPos = this._patPos + 1
                RegExpInst ei = this._emit( RegExp.OP_EOL )
                ei.w = 1
                if this._parseQuantifier()
                {
                    throw RegExpError.InvalidRepeatTarget
                }
                ret
            }
            if c == 110 || c == 116 || c == 114 || c == 102 || c == 118
            {
                # \n \t \r \f \v 控制字符
                Int32 ch = 10
                if c == 116
                {
                    ch = 9
                }
                elif c == 114
                {
                    ch = 13
                }
                elif c == 102
                {
                    ch = 12
                }
                elif c == 118
                {
                    ch = 11
                }
                this._patPos = this._patPos + 1
                Int32 atomStart = this._here()
                RegExpInst ci = this._emit( RegExp.OP_CHAR )
                ci.ch = ch
                ci.x = 1
                this._applySimpleRepeat( RegExp.OP_CHAR_REP, atomStart )
                ret
            }
            if c >= 49 && c <= 57
            {
                # \1..\9 反向引用（复杂原子：可加量词）
                this._patPos = this._patPos + 1
                Int32 g = c - 48
                if g > this._groupCount
                {
                    throw RegExpError.InvalidGroupRef
                }
                Int32 splitPh = this._here()
                this._emit( RegExp.OP_NOP )
                Int32 guardPh = this._here()
                this._emit( RegExp.OP_NOP )
                Int32 atomStart = this._here()
                RegExpInst bi = this._emit( RegExp.OP_BACKREF )
                bi.x = g
                Int32 atomEnd = this._here()
                if this._parseQuantifier()
                {
                    this._applyComplexRepeat( splitPh, guardPh, atomStart, atomEnd )
                }
                ret
            }
            if c == 48
            {
                # \0 不支持
                throw RegExpError.InvalidEscape
            }
            if ( c >= 33 && c <= 47 ) || ( c >= 58 && c <= 64 ) || ( c >= 91 && c <= 96 ) || ( c >= 123 && c <= 126 )
            {
                #转义标点 → 字面字符
                this._patPos = this._patPos + 1
                Int32 atomStart = this._here()
                RegExpInst ci = this._emit( RegExp.OP_CHAR )
                ci.ch = c
                ci.x = 1
                this._applySimpleRepeat( RegExp.OP_CHAR_REP, atomStart )
                ret
            }
            #其余（未定义字母转义 / 非 ASCII）均不支持
            throw RegExpError.InvalidEscape
        }

        # ---- 字符类编译：进入时 '[' 未消费 ----
        void _compileCharClassAtom() throws
        {
            this._patPos = this._patPos + 1
            RegExpCharSet cs = RegExpCharSet()
            Int32 cp0 = this._patPos
            if this._patAt( cp0 ) == 94
            {
                cs._negate = true
                this._patPos = this._patPos + 1
            }
            bool closed = false
            bool first = true
            Int32 prevCp = -1
            bool prevIsChar = false
            while this._patPos < this._patLen
            {
                Int32 cpp = this._patPos
                Int32 c = this._patAt( cpp )
                if c == 93
                {
                    #']'：首个位置当字面，否则收口
                    if first
                    {
                        cs.addChar( 93 )
                        prevCp = 93
                        prevIsChar = true
                        this._patPos = this._patPos + 1
                    }
                    else
                    {
                        this._patPos = this._patPos + 1
                        closed = true
                        break
                    }
                }
                elif c == 45
                {
                    #'-'：前有字面且后非 ']' → 区间；否则字面
                    Int32 nxp = this._patPos + 1
                    Int32 nx = this._patAt( nxp )
                    if prevIsChar && nx != 93 && nx >= 0
                    {
                        this._patPos = this._patPos + 1
                        Int32 hdp = this._patPos
                        Int32 hi = this._decodePatChar( hdp )
                        this._patPos = this._patPos + this._decLen
                        if hi < prevCp
                        {
                            throw RegExpError.InvalidCharClass
                        }
                        cs.addRange( prevCp, hi )
                        prevIsChar = false
                        prevCp = -1
                    }
                    else
                    {
                        cs.addChar( 45 )
                        prevCp = 45
                        prevIsChar = true
                        this._patPos = this._patPos + 1
                    }
                }
                elif c == 92
                {
                    #类内转义
                    Int32 escp = this._patPos + 1
                    Int32 e = this._patAt( escp )
                    if e == 100
                    {
                        cs.addDigit()
                        prevIsChar = false
                        prevCp = -1
                    }
                    elif e == 119
                    {
                        cs.addWord()
                        prevIsChar = false
                        prevCp = -1
                    }
                    elif e == 115
                    {
                        cs.addSpace()
                        prevIsChar = false
                        prevCp = -1
                    }
                    elif e == 68 || e == 87 || e == 83
                    {
                        #类内不支持否定简写
                        throw RegExpError.InvalidEscape
                    }
                    elif e == 98
                    {
                        #类内 \b = 退格（Python 语义）
                        cs.addChar( 8 )
                        prevCp = 8
                        prevIsChar = true
                    }
                    elif e == 110
                    {
                        cs.addChar( 10 )
                        prevCp = 10
                        prevIsChar = true
                    }
                    elif e == 116
                    {
                        cs.addChar( 9 )
                        prevCp = 9
                        prevIsChar = true
                    }
                    elif e == 114
                    {
                        cs.addChar( 13 )
                        prevCp = 13
                        prevIsChar = true
                    }
                    elif e == 102
                    {
                        cs.addChar( 12 )
                        prevCp = 12
                        prevIsChar = true
                    }
                    elif e == 118
                    {
                        cs.addChar( 11 )
                        prevCp = 11
                        prevIsChar = true
                    }
                    elif e == 66 || e == 65 || e == 122
                    {
                        #类内不支持 \B \A \z
                        throw RegExpError.InvalidEscape
                    }
                    elif e >= 0 && e < 128
                    {
                        #其余 ASCII（含数字/标点）→ 字面
                        cs.addChar( e )
                        prevCp = e
                        prevIsChar = true
                    }
                    else
                    {
                        throw RegExpError.InvalidEscape
                    }
                    this._patPos = this._patPos + 2
                }
                else
                {
                    #普通字符 / 多字节序列
                    Int32 dcp = this._patPos
                    Int32 cp = this._decodePatChar( dcp )
                    this._patPos = this._patPos + this._decLen
                    cs.addChar( cp )
                    prevCp = cp
                    prevIsChar = true
                }
                first = false
            }
            if !closed
            {
                throw RegExpError.InvalidCharClass
            }
            if this._ignoreCase
            {
                cs.applyIgnoreCase()
            }
            Int32 atomStart = this._here()
            RegExpInst ci = this._emit( RegExp.OP_CLASS )
            ci.x = this._charsets.length
            this._charsets.add( cs )
            this._applySimpleRepeat( RegExp.OP_CLASS_REP, atomStart )
        }

        # ---- 复杂原子（分组/反向引用）量词展开 ----
        #进入时体已发射于 [atomStart, atomEnd) 且 _prog 末尾 == atomEnd
        #splitPh/guardPh 为体前两个 NOP 占位；按量词矩阵回填/追加（_qMin/_qMax/_qLazy 已就绪）
        void _applyComplexRepeat( Int32 splitPh, Int32 guardPh, Int32 atomStart, Int32 atomEnd ) throws
        {
            Int32 mn = this._qMin
            Int32 mx = this._qMax
            if mn == 0 && mx == 0
            {
                #{0,0}：整体跳过
                RegExpInst sp0 = this._prog[splitPh]
                sp0.op = RegExp.OP_SPLIT
                sp0.x = atomEnd
                sp0.y = atomEnd
                ret
            }
            if mn == 0 && mx == 1
            {
                #'?'：进入体或跳过
                RegExpInst sp1 = this._prog[splitPh]
                sp1.op = RegExp.OP_SPLIT
                if this._qLazy
                {
                    sp1.x = atomEnd
                    sp1.y = atomStart
                }
                else
                {
                    sp1.x = atomStart
                    sp1.y = atomEnd
                }
                ret
            }
            if mn == 0 && mx == -1
            {
                #'*'：guardPh←GUARD，体尾 JMP 回 splitPh，splitPh←SPLIT(guardPh, after)
                RegExpInst gp0 = this._prog[guardPh]
                gp0.op = RegExp.OP_GUARD
                gp0.v = this._newGuard()
                RegExpInst jp0 = this._emit( RegExp.OP_JMP )
                jp0.x = splitPh
                Int32 after0 = this._here()
                RegExpInst sp2 = this._prog[splitPh]
                sp2.op = RegExp.OP_SPLIT
                if this._qLazy
                {
                    sp2.x = after0
                    sp2.y = guardPh
                }
                else
                {
                    sp2.x = guardPh
                    sp2.y = after0
                }
                ret
            }
            if mn == 1 && mx == -1
            {
                #'+'：guardPh←GUARD，体尾 SPLIT(guardPh, after)
                RegExpInst gp1 = this._prog[guardPh]
                gp1.op = RegExp.OP_GUARD
                gp1.v = this._newGuard()
                RegExpInst sp3 = this._emit( RegExp.OP_SPLIT )
                Int32 after1 = this._here()
                if this._qLazy
                {
                    sp3.x = after1
                    sp3.y = guardPh
                }
                else
                {
                    sp3.x = guardPh
                    sp3.y = after1
                }
                ret
            }
            if mx == mn
            {
                #{n,n} n>=1：(n-1) 份平拷
                Int32 ci1 = 1
                while ci1 < mn
                {
                    this._copyBlock( atomStart, atomEnd )
                    ci1 = ci1 + 1
                }
                ret
            }
            if mx == -1
            {
                #{n,} n>=2：(n-2) 平拷 + 新 GUARD + 1 份体拷贝 + 尾 SPLIT(guardPos, after)
                Int32 ci2 = 2
                while ci2 < mn
                {
                    this._copyBlock( atomStart, atomEnd )
                    ci2 = ci2 + 1
                }
                Int32 guardPos = this._here()
                RegExpInst gp2 = this._emit( RegExp.OP_GUARD )
                gp2.v = this._newGuard()
                this._copyBlock( atomStart, atomEnd )
                RegExpInst sp4 = this._emit( RegExp.OP_SPLIT )
                Int32 after2 = this._here()
                if this._qLazy
                {
                    sp4.x = after2
                    sp4.y = guardPos
                }
                else
                {
                    sp4.x = guardPos
                    sp4.y = after2
                }
                ret
            }
            if mn == 0
            {
                #{0,m} m>=2：splitPh←首个 SPLIT，(m-1) 份拷贝各前置占位，统一回填 afterAll
                List<Int32> splitPos = List<Int32>()
                List<Int32> bodyPos = List<Int32>()
                RegExpInst sp5 = this._prog[splitPh]
                sp5.op = RegExp.OP_SPLIT
                splitPos.add( splitPh )
                bodyPos.add( atomStart )
                Int32 ci3 = 1
                while ci3 < mx
                {
                    Int32 ph = this._here()
                    this._emit( RegExp.OP_NOP )
                    Int32 bpos = this._copyBlock( atomStart, atomEnd )
                    splitPos.add( ph )
                    bodyPos.add( bpos )
                    ci3 = ci3 + 1
                }
                Int32 afterAll = this._here()
                Int32 cj3 = 0
                while cj3 < splitPos.length
                {
                    Int32 spIdx = splitPos[cj3]
                    RegExpInst spx = this._prog[spIdx]
                    if this._qLazy
                    {
                        spx.x = afterAll
                        spx.y = bodyPos[cj3]
                    }
                    else
                    {
                        spx.x = bodyPos[cj3]
                        spx.y = afterAll
                    }
                    cj3 = cj3 + 1
                }
                ret
            }
            #{n,m} 1<=n<m：(n-1) 平拷 + (m-n) 份可选（各前置占位），统一回填 afterAll
            Int32 di = 1
            while di < mn
            {
                this._copyBlock( atomStart, atomEnd )
                di = di + 1
            }
            List<Int32> splitPos2 = List<Int32>()
            List<Int32> bodyPos2 = List<Int32>()
            Int32 dk = mn
            while dk < mx
            {
                Int32 ph2 = this._here()
                this._emit( RegExp.OP_NOP )
                Int32 bpos2 = this._copyBlock( atomStart, atomEnd )
                splitPos2.add( ph2 )
                bodyPos2.add( bpos2 )
                dk = dk + 1
            }
            Int32 afterAll2 = this._here()
            Int32 dj = 0
            while dj < splitPos2.length
            {
                Int32 spIdx2 = splitPos2[dj]
                RegExpInst spy = this._prog[spIdx2]
                if this._qLazy
                {
                    spy.x = afterAll2
                    spy.y = bodyPos2[dj]
                }
                else
                {
                    spy.x = bodyPos2[dj]
                    spy.y = afterAll2
                }
                dj = dj + 1
            }
        }

        # ---- 块拷贝：把 _prog[from, to) 追加到末尾，返回拷贝基址 ----
        #SPLIT/JMP/LAHEAD 的 x/y 指向块内 → 平移 delta；GUARD 分配新槽；其余字段原样
        Int32 _copyBlock( Int32 from, Int32 to )
        {
            Int32 basePos = this._here()
            Int32 delta = basePos - from
            Int32 i = from
            while i < to
            {
                RegExpInst src = this._prog[i]
                RegExpInst dst = this._emit( src.op )
                dst.ch = src.ch
                dst.z = src.z
                dst.w = src.w
                dst.v = src.v
                Int32 op = src.op
                if op == RegExp.OP_SPLIT || op == RegExp.OP_LAHEAD_POS || op == RegExp.OP_LAHEAD_NEG
                {
                    if src.x >= from && src.x < to
                    {
                        dst.x = src.x + delta
                    }
                    else
                    {
                        dst.x = src.x
                    }
                    if src.y >= from && src.y < to
                    {
                        dst.y = src.y + delta
                    }
                    else
                    {
                        dst.y = src.y
                    }
                }
                elif op == RegExp.OP_JMP
                {
                    if src.x >= from && src.x < to
                    {
                        dst.x = src.x + delta
                    }
                    else
                    {
                        dst.x = src.x
                    }
                    dst.y = src.y
                }
                elif op == RegExp.OP_GUARD
                {
                    dst.x = src.x
                    dst.y = src.y
                    dst.v = this._newGuard()
                }
                else
                {
                    dst.x = src.x
                    dst.y = src.y
                }
                i = i + 1
            }
            ret basePos
        }

        # ---- 执行器 ----
        #词字节判断（\w 语义：ASCII 数字/字母/下划线 + 任意多字节首字节）
        Int32 _isWordByte( Int32 b )
        {
            if ( b >= 48 && b <= 57 ) || ( b >= 65 && b <= 90 ) || ( b >= 97 && b <= 122 ) || b == 95 || b >= 128
            {
                ret 1
            }
            ret 0
        }

        #REP 指令吃进 pos 处一个字符：成功 1（st._clen 已更新），失败 0
        Int32 _repEat( RegExpState st, RegExpInst inst, Int32 pos )
        {
            if !st.readChar( pos )
            {
                ret 0
            }
            Int32 cp = this._foldCp( st._cp )
            if inst.op == RegExp.OP_CHAR_REP
            {
                if cp != inst.ch
                {
                    ret 0
                }
            }
            elif inst.op == RegExp.OP_CLASS_REP
            {
                Int32 csx = inst.x
                RegExpCharSet cs = this._charsets[csx]
                if !cs.contains( cp )
                {
                    ret 0
                }
            }
            else
            {
                if st._cp == 10
                {
                    ret 0
                }
            }
            ret 1
        }

        #回溯执行：endPc<0 主链模式（OP_MATCH 终止）；endPc>=0 子链模式（到达 endPc 即成功）
        Int32 _run( RegExpState st, Int32 pc, Int32 sp, Int32 endPc )
        {
            while true
            {
                if endPc >= 0 && pc == endPc
                {
                    ret 1
                }
                RegExpInst inst = this._prog[pc]
                Int32 op = inst.op
                if op == RegExp.OP_CHAR
                {
                    if !st.readChar( sp )
                    {
                        ret 0
                    }
                    if this._foldCp( st._cp ) != inst.ch
                    {
                        ret 0
                    }
                    sp = sp + st._clen
                    pc = pc + 1
                }
                elif op == RegExp.OP_CLASS
                {
                    if !st.readChar( sp )
                    {
                        ret 0
                    }
                    Int32 fc = this._foldCp( st._cp )
                    Int32 csx = inst.x
                    RegExpCharSet cs = this._charsets[csx]
                    if !cs.contains( fc )
                    {
                        ret 0
                    }
                    sp = sp + st._clen
                    pc = pc + 1
                }
                elif op == RegExp.OP_DOT
                {
                    if !st.readChar( sp )
                    {
                        ret 0
                    }
                    if st._cp == 10
                    {
                        ret 0
                    }
                    sp = sp + st._clen
                    pc = pc + 1
                }
                elif op == RegExp.OP_CHAR_REP || op == RegExp.OP_CLASS_REP || op == RegExp.OP_DOT_REP
                {
                    #量词：先满足 min 次，再按贪婪/懒惰回溯
                    Int32 pos = sp
                    Int32 cnt = 0
                    while cnt < inst.y
                    {
                        if this._repEat( st, inst, pos ) == 0
                        {
                            ret 0
                        }
                        pos = pos + st._clen
                        cnt = cnt + 1
                    }
                    if inst.w == 0
                    {
                        #贪婪：尽量吃进，再逐字符回退重试
                        while inst.z < 0 || cnt < inst.z
                        {
                            if this._repEat( st, inst, pos ) == 0
                            {
                                break
                            }
                            pos = pos + st._clen
                            cnt = cnt + 1
                        }
                        while true
                        {
                            if this._run( st, pc + 1, pos, endPc ) == 1
                            {
                                ret 1
                            }
                            if cnt == inst.y
                            {
                                ret 0
                            }
                            pos = st.backChar( pos )
                            cnt = cnt - 1
                        }
                    }
                    else
                    {
                        #懒惰：先试当前，失败再逐个吃进
                        while true
                        {
                            if this._run( st, pc + 1, pos, endPc ) == 1
                            {
                                ret 1
                            }
                            if inst.z >= 0 && cnt >= inst.z
                            {
                                ret 0
                            }
                            if this._repEat( st, inst, pos ) == 0
                            {
                                ret 0
                            }
                            pos = pos + st._clen
                            cnt = cnt + 1
                        }
                    }
                }
                elif op == RegExp.OP_SPLIT
                {
                    Array<Int32> saveSlots = st.copySlots()
                    Array<Int32> saveGuards = st.copyGuards()
                    if this._run( st, inst.x, sp, endPc ) == 1
                    {
                        ret 1
                    }
                    st.restoreSlots( saveSlots )
                    st.restoreGuards( saveGuards )
                    pc = inst.y
                }
                elif op == RegExp.OP_JMP
                {
                    pc = inst.x
                }
                elif op == RegExp.OP_SAVE
                {
                    Int32 sv = inst.x
                    st._slots[sv] = sp
                    pc = pc + 1
                }
                elif op == RegExp.OP_BOL
                {
                    Int32 ok = 0
                    if inst.w == 1
                    {
                        # \A：绝对串首
                        if sp == 0
                        {
                            ok = 1
                        }
                    }
                    else
                    {
                        if sp == 0
                        {
                            ok = 1
                        }
                        elif this._multiline && sp > 0 && st.byteAt( sp - 1 ) == 10
                        {
                            ok = 1
                        }
                    }
                    if ok == 0
                    {
                        ret 0
                    }
                    pc = pc + 1
                }
                elif op == RegExp.OP_EOL
                {
                    Int32 ok = 0
                    if inst.w == 1
                    {
                        # \z：绝对串尾
                        if sp == st._len
                        {
                            ok = 1
                        }
                    }
                    else
                    {
                        if sp == st._len
                        {
                            ok = 1
                        }
                        elif st.byteAt( sp ) == 10
                        {
                            ok = 1
                        }
                    }
                    if ok == 0
                    {
                        ret 0
                    }
                    pc = pc + 1
                }
                elif op == RegExp.OP_WORDB || op == RegExp.OP_NWORDB
                {
                    Int32 before = 0
                    Int32 after = 0
                    if sp > 0 && this._isWordByte( st.byteAt( sp - 1 ) ) == 1
                    {
                        before = 1
                    }
                    if sp < st._len && this._isWordByte( st.byteAt( sp ) ) == 1
                    {
                        after = 1
                    }
                    Int32 atBoundary = 0
                    if before != after
                    {
                        atBoundary = 1
                    }
                    if op == RegExp.OP_WORDB
                    {
                        if atBoundary == 0
                        {
                            ret 0
                        }
                    }
                    else
                    {
                        if atBoundary == 1
                        {
                            ret 0
                        }
                    }
                    pc = pc + 1
                }
                elif op == RegExp.OP_BACKREF
                {
                    Int32 s = st._slots[inst.x * 2]
                    Int32 e = st._slots[inst.x * 2 + 1]
                    if s < 0 || e < 0 || e < s
                    {
                        #组未参与匹配 → 反向引用失败
                        ret 0
                    }
                    Int32 blen = e - s
                    if sp + blen > st._len
                    {
                        ret 0
                    }
                    Int32 i = 0
                    Int32 fail = 0
                    while i < blen
                    {
                        if st.byteAt( s + i ) != st.byteAt( sp + i )
                        {
                            fail = 1
                            break
                        }
                        i = i + 1
                    }
                    if fail == 1
                    {
                        ret 0
                    }
                    sp = sp + blen
                    pc = pc + 1
                }
                elif op == RegExp.OP_GUARD
                {
                    Int32 gv = inst.v
                    if st._guards[gv] == sp
                    {
                        #同一位置空转 → 剪枝
                        ret 0
                    }
                    st._guards[gv] = sp
                    pc = pc + 1
                }
                elif op == RegExp.OP_LAHEAD_POS
                {
                    Array<Int32> saveSlots = st.copySlots()
                    Array<Int32> saveGuards = st.copyGuards()
                    if this._run( st, inst.x, sp, inst.y ) == 1
                    {
                        #成功：保留子链捕获（不消耗字符），继续主流程
                        pc = inst.y
                    }
                    else
                    {
                        st.restoreSlots( saveSlots )
                        st.restoreGuards( saveGuards )
                        ret 0
                    }
                }
                elif op == RegExp.OP_LAHEAD_NEG
                {
                    Array<Int32> saveSlots = st.copySlots()
                    Array<Int32> saveGuards = st.copyGuards()
                    if this._run( st, inst.x, sp, inst.y ) == 1
                    {
                        st.restoreSlots( saveSlots )
                        st.restoreGuards( saveGuards )
                        ret 0
                    }
                    st.restoreSlots( saveSlots )
                    st.restoreGuards( saveGuards )
                    pc = inst.y
                }
                elif op == RegExp.OP_MATCH
                {
                    ret 1
                }
                else
                {
                    #NOP 等直接跳过
                    pc = pc + 1
                }
            }
        }

        # ---- C 下沉入口 ----
        #flags 位打包：bit0=ignoreCase, bit1=multiline, bit2=anchored
        Int32 _flags( bool anchored )
        {
            Int32 f = 0
            if this._ignoreCase
            {
                f = f + 1
            }
            if this._multiline
            {
                f = f + 2
            }
            if anchored
            {
                f = f + 4
            }
            ret f
        }

        #惰性展平指令链：每指令 7 槽 [op, ch, x, y, z, w, v]
        Array<Int32> _flatProg()
        {
            if this._flatProgCache != null
            {
                ret this._flatProgCache
            }
            Int32 n = this._prog.length
            Array<Int32> res = Array<Int32>( n * 7 )
            Int32 i = 0
            while i < n
            {
                RegExpInst inst = this._prog[i]
                Int32 b = i * 7
                res[b] = inst.op
                res[b + 1] = inst.ch
                res[b + 2] = inst.x
                res[b + 3] = inst.y
                res[b + 4] = inst.z
                res[b + 5] = inst.w
                res[b + 6] = inst.v
                i = i + 1
            }
            this._flatProgCache = res
            ret res
        }

        #惰性展平字符集段表：[段数, 各段偏移..., 段数据...]
        #段数据 = 128 个 ascii 标志 + negate + rangeCount + lo/hi 交错
        Array<Int32> _flatCs()
        {
            if this._flatCsCache != null
            {
                ret this._flatCsCache
            }
            Int32 segN = this._charsets.length
            Int32 total = 1 + segN
            Int32 i = 0
            while i < segN
            {
                RegExpCharSet cs = this._charsets[i]
                List<Int32> los = cs._loList
                total = total + 130 + los.length * 2
                i = i + 1
            }
            Array<Int32> res = Array<Int32>( total )
            res[0] = segN
            Int32 off = 1 + segN
            i = 0
            while i < segN
            {
                res[1 + i] = off
                RegExpCharSet cs = this._charsets[i]
                Array<Int32> ascii = cs._ascii
                Int32 j = 0
                while j < 128
                {
                    res[off + j] = ascii[j]
                    j = j + 1
                }
                if cs._negate
                {
                    res[off + 128] = 1
                }
                else
                {
                    res[off + 128] = 0
                }
                List<Int32> los = cs._loList
                List<Int32> his = cs._hiList
                Int32 rn = los.length
                res[off + 129] = rn
                Int32 k = 0
                while k < rn
                {
                    res[off + 130 + k * 2] = los[k]
                    res[off + 131 + k * 2] = his[k]
                    k = k + 1
                }
                off = off + 130 + rn * 2
                i = i + 1
            }
            this._flatCsCache = res
            ret res
        }

        #经 C 下沉执行（vm_sys_regexp_run 一次完成整个回溯匹配）
        #anchored=true 只试 from 一处（match 语义）；返回 RegExpMatch 或 null
        RegExpMatch _runOnCvm( string text, Int32 from, bool anchored )
        {
            Int32 gc = this._groupCount
            Array<Int32> slots = Array<Int32>( ( gc + 1 ) * 2 )
            Int32 ms = SystemRegExpRun( this._flatProg(), this._flatCs(), text, from, this._flags( anchored ), slots, this._guardCount )
            if ms < 0
            {
                ret null
            }
            ret RegExpMatch( text, slots[0], slots[1], slots, gc )
        }

        #经 SL 解释器执行（性能基线对照；驱动语义与 C 下沉版一致：逐起点尝试 + 每起点新建状态）
        RegExpMatch _runOnSl( string text, Int32 from, bool anchored )
        {
            Int32 gc = this._groupCount
            Int32 slotCount = ( gc + 1 ) * 2
            Int32 len = SystemStringLength( text )
            Int32 pos = from
            while pos <= len
            {
                RegExpState st = RegExpState( text, slotCount, this._guardCount )
                if this._run( st, 0, pos, -1 ) == 1
                {
                    ret RegExpMatch( text, st._slots[0], st._slots[1], st._slots, gc )
                }
                if anchored
                {
                    break
                }
                pos = pos + 1
            }
            ret null
        }

        # ---- 公有 API ----
        #从 from 起扫描首个匹配；无匹配返回 null
        RegExpMatch _searchFrom( string text, Int32 from )
        {
            if text == null
            {
                ret null
            }
            Int32 len = SystemStringLength( text )
            if from < 0
            {
                from = 0
            }
            if from > len
            {
                from = len
            }
            ret this._runOnCvm( text, from, false )
        }

        #锚定匹配（串首起，不扫描）；失败返回 null
        public RegExpMatch match( string text )
        {
            if text == null
            {
                ret null
            }
            ret this._runOnCvm( text, 0, true )
        }

        #扫描首个匹配；无匹配返回 null
        public RegExpMatch search( string text )
        {
            ret this._searchFrom( text, 0 )
        }

        #是否存在匹配
        public bool isMatch( string text )
        {
            ret this._searchFrom( text, 0 ) != null
        }

        #所有非重叠匹配（含零宽匹配，按 finditer 语义推进游标）
        public List<RegExpMatch> matchAll( string text )
        {
            List<RegExpMatch> res = List<RegExpMatch>()
            if text == null
            {
                ret res
            }
            Int32 len = SystemStringLength( text )
            Int32 pos = 0
            while pos <= len
            {
                RegExpMatch m = this._searchFrom( text, pos )
                if m == null
                {
                    break
                }
                res.add( m )
                if m.end > m.index
                {
                    pos = m.end
                }
                else
                {
                    pos = m.index + 1
                }
            }
            ret res
        }

        #展开替换模板：$& 整体 / $1..$9 组 / $$ 字面 $
        string _expand( string rep, RegExpMatch m )
        {
            string res = ""
            Int32 n = SystemStringLength( rep )
            Int32 i = 0
            while i < n
            {
                Int32 c = SystemStringCharCodeAt( rep, i )
                if c == 36 && i + 1 < n
                {
                    Int32 d = SystemStringCharCodeAt( rep, i + 1 )
                    if d == 36
                    {
                        res = res + "\$"
                        i = i + 2
                        continue
                    }
                    if d == 38
                    {
                        res = res + m.value()
                        i = i + 2
                        continue
                    }
                    if d >= 49 && d <= 57
                    {
                        Int32 g = d - 48
                        if g <= this._groupCount
                        {
                            res = res + m.group( g )
                        }
                        i = i + 2
                        continue
                    }
                }
                res = res + SystemStringRange( rep, i, i + 1 )
                i = i + 1
            }
            ret res
        }

        #替换所有匹配（rep 支持 $& $1..$9 $$）；零宽匹配保留原字符
        public string replace( string text, string rep )
        {
            if text == null
            {
                ret ""
            }
            if rep == null
            {
                rep = ""
            }
            Int32 len = SystemStringLength( text )
            Int32 last = 0
            Int32 pos = 0
            string res = ""
            while pos <= len
            {
                RegExpMatch m = this._searchFrom( text, pos )
                if m == null
                {
                    break
                }
                res = res + SystemStringRange( text, last, m.index ) + this._expand( rep, m )
                if m.end > m.index
                {
                    last = m.end
                    pos = m.end
                }
                else
                {
                    #零宽匹配：last 不动（原字符留给下轮），游标前进
                    last = m.index
                    pos = m.index + 1
                }
            }
            res = res + SystemStringRange( text, last, len )
            ret res
        }

        #按匹配切分（Python re.split 语义变体：零宽匹配仅在产生新内容处设切点）
        public List<string> split( string text )
        {
            List<string> res = List<string>()
            if text == null
            {
                ret res
            }
            Int32 len = SystemStringLength( text )
            Int32 last = 0
            Int32 pos = 0
            while pos <= len
            {
                RegExpMatch m = this._searchFrom( text, pos )
                if m == null
                {
                    break
                }
                if m.end > m.index
                {
                    res.add( SystemStringRange( text, last, m.index ) )
                    last = m.end
                    pos = m.end
                }
                else
                {
                    if m.index > last
                    {
                        res.add( SystemStringRange( text, last, m.index ) )
                        last = m.index
                    }
                    pos = m.index + 1
                }
            }
            if last < len || res.length == 0
            {
                res.add( SystemStringRange( text, last, len ) )
            }
            ret res
        }

        #捕获组数量（不含组 0）
        public get Int32 groupCount()
        {
            ret this._groupCount
        }

        # ---- 静态便捷 ----
        public static bool isMatch( string text, string pattern ) throws
        {
            RegExp re = RegExp( pattern )
            ret re.isMatch( text )
        }

        public static bool isMatch( string text, string pattern, bool ignoreCase ) throws
        {
            RegExp re = RegExp( pattern, ignoreCase )
            ret re.isMatch( text )
        }

        public static RegExpMatch match( string text, string pattern ) throws
        {
            RegExp re = RegExp( pattern )
            ret re.match( text )
        }

        public static RegExpMatch search( string text, string pattern ) throws
        {
            RegExp re = RegExp( pattern )
            ret re.search( text )
        }

        public static List<RegExpMatch> matchAll( string text, string pattern ) throws
        {
            RegExp re = RegExp( pattern )
            ret re.matchAll( text )
        }

        public static string replace( string text, string pattern, string rep ) throws
        {
            RegExp re = RegExp( pattern )
            ret re.replace( text, rep )
        }

        #转义模式特殊字符（ASCII 标点前加 '\'；字母数字保持原样）
        public static string escape( string s )
        {
            if s == null
            {
                ret ""
            }
            string res = ""
            Int32 n = SystemStringLength( s )
            Int32 i = 0
            while i < n
            {
                Int32 c = SystemStringCharCodeAt( s, i )
                if c >= 0 && c < 128 && !( ( c >= 48 && c <= 57 ) || ( c >= 65 && c <= 90 ) || ( c >= 97 && c <= 122 ) )
                {
                    res = res + "\\" + SystemStringRange( s, i, i + 1 )
                }
                else
                {
                    res = res + SystemStringRange( s, i, i + 1 )
                }
                i = i + 1
            }
            ret res
        }
    }
}

