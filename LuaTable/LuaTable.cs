using System;using System.Collections;

namespace LuaTable{    public class LuaValue    {        public object Value;        public LuaValue(object value)        {            this.Value = value;        }        public static explicit operator int(LuaValue lv)        {            return (int)lv.Value;        }        public static explicit operator double(LuaValue lv)        {            return (double)lv.Value;        }        public static explicit operator float(LuaValue lv)        {            return (float)lv.Value;        }        public Type GetValueType()        {            return this.Value == null ? null : Value.GetType();        }        public override string ToString()        {            return Value.ToString();        }    }          public class LuaTable    {


        #region 内部类        public class TableKey        {            public LuaValue KeyValue = null;            public int next = 0;        }        public class TableNode        {            public TableKey Key = new TableKey();            public LuaValue Val = null;        }

        public class LuaTablePairs : IEnumerator, IEnumerable, IDisposable
        {
            private LuaTable table;
            private int index = 0;
            public LuaTablePairs(LuaTable t)
            {
                this.table = t;
            }

            public object Current
            {
                get
                {
                    if (table == null)
                    {
                        return null;
                    }
                    return table[index];
                }
            }

            public void Dispose()
            {
                index = 0;
            }

            public IEnumerator GetEnumerator()
            {
                return (IEnumerator)this;
            }

            public bool MoveNext()
            {
                if (table == null)
                {
                    return false;
                }

                return table[++index] != null;
            }

            public void Reset()
            {
                index = 0;
            }
        }

        public class LuaTableIpairs : IEnumerator, IEnumerable, IDisposable
        {
            private LuaTable table;
            private LuaValue preKey = null;
            public LuaTableIpairs(LuaTable t)
            {
                this.table = t;
            }

            public object Current
            {
                get
                {
                    if (table == null)
                    {
                        return null;
                    }
                    return table[preKey.Value];
                }
            }

            public void Dispose()
            {
                this.preKey = null;
            }

            public IEnumerator GetEnumerator()
            {
                return (IEnumerator)this;
            }

            public bool MoveNext()
            {
                if(table == null)
                {
                    return false;
                }

                int i = FindIndex(preKey);

                for (; i < table.sizeArray; ++i)
                {

                    if (table.array[i].Value != null)
                    {
                        this.preKey = new LuaValue(i + 1);
                        return true;
                    }
                }
                int sizeNode = table.nodes == null ? 0 : (1 << table.lsizeNode);
                for (i -= table.sizeArray; i < sizeNode; i++)
                {
                    if(!table.NodeIsDummy(table.nodes[i]))
                    {
                        this.preKey = table.nodes[i].Key.KeyValue;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                this.preKey = null;
            }

            private int ArrayIndex(LuaValue key)
            {
                if (key.GetValueType() == typeof(int))
                {
                    return (int)key;
                }
                return 0;
            }
            private int FindIndex(LuaValue key)
            {
                int i;
                if (key == null || key.Value == null)
                {
                    return 0;
                }

                i = this.ArrayIndex(key);

                if (i != 0 && i <= table.sizeArray)
                {
                    return i;
                }
                else
                {
                    int nx;
                    int n = table.MainPosition(key);
                    for (;;)
                    {
                        if (table.nodes[n].Key.KeyValue.Value == key.Value)
                        {
                            return (n + 1) + table.sizeArray;
                        }
                        nx = table.nodes[n].Key.next;
                        if (nx == 0)
                        {
                            throw new Exception("invalid key to 'next'");
                        }
                        else n += nx;
                    }
                }
            }

        }






        #endregion
        #region 支持LuaValue        private LuaValue tovalue;        public static implicit operator LuaValue(LuaTable table)        {            return table.tovalue;        }


        #endregion

        private byte lsizeNode;        private int sizeArray;        private LuaValue[] array;        private TableNode[] nodes;        private int lastFree;        private LuaTable metaTable;        public LuaTable()        {            this.array = null;            this.sizeArray = 0;            this.metaTable = null;            this.SetNodeVector(0);            this.tovalue = new LuaValue(this);            this.pairs = new LuaTablePairs(this);            this.ipairs = new LuaTableIpairs(this);        }        public object this[object key]        {            get { var val = this.GetValue(new LuaValue(key)); return val == null ? null : val.Value; }            set { this.SetValue(new LuaValue(key), new LuaValue(value)); }        }

        /// <summary>
        /// 获取长度
        /// </summary>
        /// <returns></returns>
        public int GetLength()
        {
            int j = this.sizeArray;
            if (j > 0 && this.array[j - 1].Value == null)
            {
                /* 二分搜索 */
                int i = 0;
                while (j - i > 1)
                {
                    int m = (i + j) / 2;
                    if (this.array[m - 1].Value == null) j = m;
                    else i = m;
                }
                return i;
            }
            else if (j == 0) 
                return j;
            else return UnboundSearch(j);
        }

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="obj"></param>
        public void Insert(object obj)
        {
            int length = this.GetLength();
            this[length + 1] = obj;
        }

        public void Insert(object obj, int pos)
        {
            int length = this.GetLength();
            if(pos >= 1 && pos <= length)
            {
                for(int i = length + 1; i > pos; --i)
                {
                    this[i] = this[i - 1];
                }
                this[pos] = obj;
            }
        }

        /// <summary>
        /// 移除
        /// </summary>
        public object Remove(int pos)
        {
            int length = this.GetLength();
            if (pos >= 1 && pos <= length)
            {
                object val = this[pos];
                for(; pos < length; ++ pos)
                {
                    this[pos] = this[pos + 1];
                }
                return val;
            }
            return null;
        }

        private LuaTablePairs pairs;
        private LuaTableIpairs ipairs;
        public static LuaTablePairs Pairs(LuaTable table)
        {
            if(table == null)
            {
                return null;
            }
            return table.pairs;
        }

        public static LuaTableIpairs Ipairs(LuaTable table)
        {
            if (table == null)
            {
                return null;
            }
            return table.ipairs;
        }




        private int UnboundSearch(int j)
        {
            int i = j;
            j++;
            while (this.GetIntValue(j) != null)
            {
                i = j;
                if (j > int.MaxValue / 2)
                {
                    i = 1;
                    while (this.GetIntValue(i) != null) i++;
                    return i - 1;
                }
                j *= 2;
            }
            while(j - i > 1)
            {
                int m = (i + j) / 2;
                if (this.GetIntValue(m) == null) j = m;
                else i = m;
            }
            return i;
        }

        /// <summary>        /// 设置数组部分的大小        /// </summary>        private void SetArrayVector(int size)        {            LuaValue[] na = size == 0 ? null : new LuaValue[size];
            //拷贝之前的元素
            for (int i = 0; ; ++i)            {                if (i >= size) break;                na[i] = i < this.sizeArray ? this.array[i] : new LuaValue(null);            }            this.array = na;            this.sizeArray = (int)size;        }

        /// <summary>        /// 设置hash部分的大小        /// </summary>        private void SetNodeVector(int size)        {            if (size == 0)            {                this.lsizeNode = 0;                this.lastFree = -1;                this.nodes = null;            }            else            {                int lsize = (int)Math.Ceiling(Math.Log(size, 2));                size = 1 << lsize;                this.nodes = new TableNode[size];                this.lsizeNode = (byte)lsize;                this.lastFree = (int)size - 1;            }        }

        /// <summary>        /// get 方法(ps: 这里没有写shortstring的hash-case)        /// </summary>        /// <returns></returns>        private LuaValue GetValue(LuaValue key)        {            if (key.GetValueType() == null)            {                return null;            }            else if (key.GetValueType() == typeof(int))            {                return this.GetIntValue((int)key);            }            else if (key.GetValueType() == typeof(float) || key.GetValueType() == typeof(double))            {                bool beInt = false;                int val = 0;                if (key.GetValueType() == typeof(float))                {                    float fValue = (float)key;                    if ((int)fValue == fValue)                    {                        beInt = true;                        val = (int)fValue;                    }                }                else                {                    double dbValue = (double)key;                    if ((int)dbValue == dbValue)                    {                        beInt = true;                        val = (int)dbValue;                    }                }                if (!beInt)                {                    return this.GetGenericValue(key);                }                else                {                    return this.GetIntValue(val);                }            }            else            {                return this.GetGenericValue(key);            }        }

        /// <summary>        /// set 方法        /// </summary>        private void SetValue(LuaValue key, LuaValue value)        {            LuaValue v = this.GetOrCreateValue(key);            v.Value = value.Value;        }

        /// <summary>        /// 如果不存在就创建        /// </summary>        private LuaValue GetOrCreateValue(LuaValue key)        {            LuaValue v = this.GetValue(key);            if (v != null)            {                return v;            }            return this.NewKey(key);        }

        /// <summary>        /// 创建一个新的key        /// </summary>        private LuaValue NewKey(LuaValue key)        {            TableNode mp;            if (key == null)            {                return null;            }            else if (key.GetValueType() == typeof(float))            {                float fvalue = (float)key;                if (fvalue <= (int)fvalue)                {                    key.Value = (int)fvalue;                }            }            else if (key.GetValueType() == typeof(double))            {                double dbValue = (double)key;                if (dbValue <= (int)dbValue)                {                    key.Value = (int)dbValue;                }            }            int nIdx = this.MainPosition(key);

            mp = this.nodes == null ? null : this.nodes[nIdx];            if (!this.NodeIsDummy(mp) || this.nodes == null)            {                int fIdx = this.GetFreePos(); /* get a free place */

                if (fIdx == -1)                {                    this.Rehash(key);                    return this.GetOrCreateValue(key);                }                int otherIdx = this.MainPosition(mp.Key.KeyValue);                TableNode othern = this.nodes[otherIdx];                if (othern != mp)                {                    while (this.nodes[otherIdx + othern.Key.next] != mp)                    {                        otherIdx += othern.Key.next;                        othern = this.nodes[otherIdx];                    }                    othern.Key.next = fIdx - otherIdx;

                    //换位置
                    this.nodes[fIdx] = mp;                    if (mp.Key.next != 0)                    {                        mp.Key.next += nIdx - fIdx;                    }                    mp = new TableNode();                    this.nodes[nIdx] = mp;                }                else                {                    this.nodes[fIdx] = new TableNode();                    if (mp.Key.next != 0)                    {                        this.nodes[fIdx].Key.next = nIdx + mp.Key.next - fIdx;                    }                    mp.Key.next = fIdx - nIdx;                    mp = this.nodes[fIdx];                }            }            else            {                mp = new TableNode();                this.nodes[nIdx] = mp;            }            mp.Key.KeyValue = key;            mp.Val = new LuaValue(null);            return mp.Val;        }

        /// <summary>        /// 通过IntKey获取值        /// 首先去数组部分找，如果找不到去hash部分找        /// </summary>        private LuaValue GetIntValue(int key)        {            if (key <= this.sizeArray && key > 0)            {                return this.array[key - 1];            }            else            {                int nIdx = this.MainPosition(key);                TableNode n = this.nodes == null ? null : this.nodes[nIdx];                for (;;)                {                    if (this.NodeIsDummy(n))                    {                        return null;                    }                    if ((int)n.Key.KeyValue == key)                    {                        return n.Val;                    }                    else                    {                        int nx = n.Key.next;                        if (nx == 0) break;                        nIdx += nx;                        n = this.nodes[nIdx];                    }                }            }            return null;        }

        /// <summary>        /// 设置一个intkey的值        /// </summary>        private void SetIntValue(int key, LuaValue value)        {            LuaValue p = this.GetIntValue(key);            if (p == null)            {                p = this.NewKey(new LuaValue(key));            }            p.Value = value.Value;        }

        /// <summary>        /// 计算hash，获取对应值        /// </summary>        private LuaValue GetGenericValue(LuaValue key)        {            int nIdx = this.MainPosition(key.Value);            TableNode n = this.nodes == null ? null : this.nodes[nIdx];            for (;;)            {                if (this.NodeIsDummy(n))                {                    return null;                }                if (n.Key.KeyValue.Value.Equals(key.Value))                    return n.Val;                else                {                    int nx = n.Key.next;                    if (nx == 0) break;                    nIdx += nx;                    n = this.nodes[nIdx];                }            }            return null;        }

        /// <summary>        /// 重新分配hash表        /// </summary>        private void Rehash(LuaValue key)        {            int asize;            int na;            int totaluse;            int[] nums = new int[32];            na = this.NumuseArray(nums);            totaluse = na;            totaluse += this.NumuseHash(nums, ref na);            na += this.CountInt(key, nums);            totaluse++;            asize = this.ComputeSizes(nums, ref na);            this.Resize(asize, totaluse - na);        }

        /// <summary>        /// 得到下一个空闲的位置        /// </summary>        private int GetFreePos()        {            while (this.lastFree >= 0)            {                if (this.NodeIsDummy(this.nodes[this.lastFree]))                {                    return this.lastFree--;                }                this.lastFree--;            }            return -1;        }

        /// <summary>        /// 获得mainposition        /// </summary>        private int MainPosition(LuaValue value)        {            int hashValue = Hash(value.Value);            return Math.Abs(hashValue) % (1 << this.lsizeNode);        }        private int MainPosition(object value)        {            int hashValue = Hash(value);            return Math.Abs(hashValue) % (1 << this.lsizeNode);        }


        /// <summary>        /// hash函数，没有按照lua里面的方式，随意的写的        /// </summary>        private int Hash<T>(T value)        {            return value.GetHashCode();        }


        private bool NodeIsDummy(TableNode node)
        {
            if(node == null || node.Val == null || node.Val.Value == null)
            {
                return true;
            }
            return false;
        }


        /// <summary>        /// 计算数组区间，整数key的个数，以及在各个2^(k-1)~2^k区间里面的个数        /// </summary>        private int NumuseArray(int[] nums)        {            int lg = 0;            int ttlg = 1;            int ause = 0;            int i = 1;            for (; lg < 32; ++lg, ttlg *= 2)            {                int lc = 0;                int lim = ttlg;                if (lim > this.sizeArray)                {                    lim = this.sizeArray;                    if (i > lim)                        break;                }                for (; i <= lim; ++i)                {                    if (this.array[i - 1] != null)                        lc++;                }                nums[lg] += lc;                ause += lc;            }            return ause;        }

        /// <summary>        /// 计算hash区间，整数key的个数，以及在各个2^(k-1)~2^k区间里面的个数        /// </summary>        private int NumuseHash(int[] nums, ref int na)        {            int totaluse = 0;            int ause = 0;            int i = this.nodes == null ? 0 : (1 << this.lsizeNode);            while (i-- > 0)            {                TableNode n = this.nodes[i];                if (n != null)                {                    ause += CountInt(n.Key.KeyValue, nums);                    totaluse++;                }            }            na += ause;            return totaluse;        }        private int CountInt(LuaValue value, int[] nums)        {            if (value.GetValueType() != typeof(int))            {                return 0;            }            int k = (int)value;            if (k > 0)            {                nums[(int)Math.Ceiling(Math.Log(k, 2))]++;                return 1;            }            return 0;        }

        /// <summary>        /// 计算最适合的array大小        /// </summary>        private int ComputeSizes(int[] nums, ref int pna)        {            int a = 0;            int na = 0;            int optimal = 0;            for (int i = 0, twotoi = 1; pna > twotoi / 2; i++, twotoi *= 2)            {                if (nums[i] > 0)                {                    a += (int)nums[i];                    if (a > twotoi / 2)                    {                        optimal = twotoi;
                        na = a;                    }                }            }            pna = na;            return optimal;        }

        /// <summary>        /// 重新设置array和hash的大小        /// </summary>        private void Resize(int nasize, int nhsize)        {            int oldasize = (int)this.sizeArray;            int oldhsize = this.nodes == null ? 0 : (1 << this.lsizeNode);            TableNode[] nold = this.nodes;  /* save old hash ... */
            if (nasize > oldasize) //array需要增长
            {                this.SetArrayVector(nasize);            }            this.SetNodeVector(nhsize);            if (nasize < oldasize)            {                this.sizeArray = (int)nasize;                for (int i = (int)nasize; i < oldasize; i++)                {
                    /* 因为上面已经修改了数组大小，所以下面的插入一定是插入的hash */
                    if (this.array[i] != null)                    {                        this.SetIntValue(i + 1, this.array[i]);                    }                }
                /* 收缩数组大小 */
                LuaValue[] temp = new LuaValue[nasize];                for (int i = 0; i < nasize; ++i)                {                    temp[i] = this.array[i];                }                this.array = temp;            }
            /* 重新插入到hash */
            for (int i = 0; i < oldhsize; ++i)            {                if (nold[i] != null)                {                    this.SetValue(nold[i].Key.KeyValue, nold[i].Val);                }            }        }    }}