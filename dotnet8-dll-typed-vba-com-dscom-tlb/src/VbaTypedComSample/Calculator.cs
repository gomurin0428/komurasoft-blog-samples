// COM に公開するインターフェイスとクラス（記事 4.3）
//
// - Guid はインターフェイスとクラスに別々に振る
// - クラスは ClassInterfaceType.None にして、自動生成クラスインターフェイスに依存しない
// - VBA で扱いやすいように InterfaceIsDual にする
// - DispId を振っておくと、公開後にメソッド順をいじったときの事故を減らしやすい
// - COM から New されるので、public な引数なしコンストラクターを用意する
using System.Runtime.InteropServices;

namespace VbaTypedComSample;

[ComVisible(true)]
[Guid("2A1BBEDE-DE6E-4C34-AD60-2E9E0E33E999")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface ICalculator
{
    [DispId(1)]
    int Add(int x, int y);

    [DispId(2)]
    double Divide(double x, double y);

    [DispId(3)]
    string Hello(string name);
}

[ComVisible(true)]
[Guid("FAD1C752-0BB6-4DDD-889F-FE446350847A")]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(ICalculator))]
public class Calculator : ICalculator
{
    public Calculator()
    {
    }

    public int Add(int x, int y) => checked(x + y);

    public double Divide(double x, double y)
    {
        if (y == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "0 では割れません。");
        }

        return x / y;
    }

    public string Hello(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Hello";
        }

        return $"Hello, {name}";
    }
}
