
public class Member extends Object
{
    public string name = "";
    public object value = null;
    public int index = -1;
    void _init_()
    {
    }
    public string toString()
    {
        string nameText = this.name == null ? "" : this.name
        string indexText = this.index.toString()
        string valueText = this.value == null ? "null" : this.value.toString()
        ret "Member{name=" + nameText + ", index=" + indexText + ", value=" + valueText + "}"
    }
}
