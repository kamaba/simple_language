# Vocab / Tokenizer —— 文本词表与分词
#
# Vocab    ：id <-> 词 双向映射，内置 <pad> <unk> <bos> <eos>
# Tokenizer：词级 / 字符级分词，提供 encode / decode / pad / truncate
#
# 说明：标准库 String 目前只有 length / range / front / end，
#       因此分词用 range(i, i+1) 逐字符扫描实现，不依赖 split/indexOf

@Nickname("Vocab")
public class Vocab
{
    public List<string> items = null
    public Map<string, Int32> index = null

    public Int32 padId = 0
    public Int32 unkId = 1
    public Int32 bosId = 2
    public Int32 eosId = 3

    public void _init_()
    {
        this.items = List<string>()
        this.index = Map<string, Int32>()
        this.add( "<pad>" )
        this.add( "<unk>" )
        this.add( "<bos>" )
        this.add( "<eos>" )
    }

    # 返回词的 id；已存在则直接返回旧 id
    public Int32 add( string word )
    {
        if this.index.containsKey( word )
        {
            ret this.index[word]
        }
        Int32 id = this.items.length
        this.items.add( word )
        this.index.add( word, id )
        ret id
    }

    public bool contains( string word )
    {
        ret this.index.containsKey( word )
    }

    public Int32 idOf( string word )
    {
        if this.index.containsKey( word )
        {
            ret this.index[word]
        }
        ret this.unkId
    }

    public string wordOf( Int32 id )
    {
        if id < 0
        {
            ret "<unk>"
        }
        if id >= this.items.length
        {
            ret "<unk>"
        }
        ret this.items[id]
    }

    public get int size()
    {
        ret this.items.length
    }

    public Array<string> toArray()
    {
        Array<string> arr = Array<string>( this.items.length )
        for i = 0, i < this.items.length, i++
        {
            arr[i] = this.items[i]
        }
        ret arr
    }
}

@Nickname("Tokenizer")
public class Tokenizer
{
    public Vocab vocab = null
    public Int32 maxLen = 128
    public bool charLevel = false

    # 预留：标准库暂未提供 toLower，后续接入后在此生效
    public bool lower = false

    public void _init_()
    {
        this.vocab = Vocab()
        this.maxLen = 128
        this.charLevel = false
    }

    public void _init_( Vocab vocab )
    {
        this.vocab = vocab
        this.maxLen = 128
        this.charLevel = false
    }

    # ── 建表 ─────────────────────────────────────────────
    # 词级：按空白切分，过滤低频词
    public void build( Array<string> texts, Int32 minFreq )
    {
        Map<string, Int32> freq = Map<string, Int32>()
        for i = 0, i < texts.length, i++
        {
            Array<string> ws = this.split( texts[i] )
            for j = 0, j < ws.length, j++
            {
                string w = ws[j]
                if freq.containsKey( w )
                {
                    freq[w] = freq[w] + 1
                }
                else
                {
                    freq.add( w, 1 )
                }
            }
        }
        List<string> keys = freq.keys()
        for i = 0, i < keys.length, i++
        {
            string w = keys[i]
            if freq[w] >= minFreq
            {
                this.vocab.add( w )
            }
        }
    }

    # 字符级：把语料里出现过的每个字符都收进词表
    public void buildChars( Array<string> texts )
    {
        this.charLevel = true
        for i = 0, i < texts.length, i++
        {
            string s = texts[i]
            for k = 0, k < s.length(), k++
            {
                this.vocab.add( s.range( k, k + 1 ) )
            }
        }
        this.vocab.add( " " )
    }

    # ── 切分 ─────────────────────────────────────────────
    public Array<string> split( string text )
    {
        List<string> out = List<string>()
        string cur = ""
        for i = 0, i < text.length(), i++
        {
            string ch = text.range( i, i + 1 )
            if ch == " "
            {
                if cur != ""
                {
                    out.add( cur )
                    cur = ""
                }
            }
            else
            {
                cur = cur + ch
            }
        }
        if cur != ""
        {
            out.add( cur )
        }
        Array<string> arr = Array<string>( out.length )
        for i = 0, i < out.length, i++
        {
            arr[i] = out[i]
        }
        ret arr
    }

    # ── 编码 / 解码 ──────────────────────────────────────
    public Array<Int32> encode( string text )
    {
        List<string> tokens = List<string>()
        if this.charLevel
        {
            for i = 0, i < text.length(), i++
            {
                tokens.add( text.range( i, i + 1 ) )
            }
        }
        else
        {
            Array<string> ws = this.split( text )
            for i = 0, i < ws.length, i++
            {
                tokens.add( ws[i] )
            }
        }
        Array<Int32> ids = Array<Int32>( tokens.length )
        for i = 0, i < tokens.length, i++
        {
            ids[i] = this.vocab.idOf( tokens[i] )
        }
        ret ids
    }

    # 带特殊符号：<bos> ... <eos>
    public Array<Int32> encodeWithSpecial( string text )
    {
        Array<Int32> body = this.encode( text )
        Array<Int32> ids = Array<Int32>( body.length + 2 )
        ids[0] = this.vocab.bosId
        for i = 0, i < body.length, i++
        {
            ids[ i + 1 ] = body[i]
        }
        ids[ body.length + 1 ] = this.vocab.eosId
        ret ids
    }

    public string decode( Array<Int32> ids )
    {
        string s = ""
        for i = 0, i < ids.length, i++
        {
            # 跳过 <pad> / <bos> / <eos>
            if ids[i] != this.vocab.padId
            {
                if ids[i] != this.vocab.bosId
                {
                    if ids[i] != this.vocab.eosId
                    {
                        s = s + this.vocab.wordOf( ids[i] )
                    }
                }
            }
        }
        ret s
    }

    # ── 长度规整 ─────────────────────────────────────────
    public Array<Int32> truncate( Array<Int32> ids, Int32 len )
    {
        if ids.length <= len
        {
            ret ids
        }
        Array<Int32> r = Array<Int32>( len )
        for i = 0, i < len, i++
        {
            r[i] = ids[i]
        }
        ret r
    }

    public Array<Int32> pad( Array<Int32> ids, Int32 len )
    {
        Array<Int32> r = Array<Int32>( len )
        for i = 0, i < len, i++
        {
            if i < ids.length
            {
                r[i] = ids[i]
            }
            else
            {
                r[i] = this.vocab.padId
            }
        }
        ret r
    }

    # 编码并补齐到 maxLen
    public Array<Int32> encodePadded( string text )
    {
        ret this.pad( this.truncate( this.encodeWithSpecial( text ), this.maxLen ), this.maxLen )
    }

    public get int vocabSize()
    {
        ret this.vocab.size()
    }
}
