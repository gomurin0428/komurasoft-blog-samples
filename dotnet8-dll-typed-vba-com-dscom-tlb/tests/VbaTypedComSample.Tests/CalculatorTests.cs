// Calculator の純粋ロジック部分のテスト
//
// COM 登録や TLB は不要で、通常の .NET クラスとしてインスタンス化して検証できます。
// （COM ホスティング経由の呼び出しと VBA からの利用は Windows 上で確認します）
using System.Runtime.InteropServices;
using Xunit;

namespace VbaTypedComSample.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(-5, 5, 0)]
    [InlineData(0, 0, 0)]
    public void Add_TwoIntegers_ReturnsSum(int x, int y, int expected)
    {
        var calc = new Calculator();

        Assert.Equal(expected, calc.Add(x, y));
    }

    [Fact]
    public void Add_Overflow_ThrowsOverflowException()
    {
        var calc = new Calculator();

        // checked(x + y) なので、黙って桁あふれせず例外になる
        Assert.Throws<OverflowException>(() => calc.Add(int.MaxValue, 1));
    }

    [Theory]
    [InlineData(10, 4, 2.5)]
    [InlineData(-10, 4, -2.5)]
    [InlineData(0, 3, 0)]
    public void Divide_NonZeroDivisor_ReturnsQuotient(double x, double y, double expected)
    {
        var calc = new Calculator();

        Assert.Equal(expected, calc.Divide(x, y), precision: 10);
    }

    [Fact]
    public void Divide_ByZero_ThrowsArgumentOutOfRangeException()
    {
        var calc = new Calculator();

        // .NET 側で投げた例外は、VBA 側では COM エラー（Err.Number / Err.Description）として見える（記事 8.1）
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => calc.Divide(10, 0));
        Assert.Equal("y", ex.ParamName);
    }

    [Theory]
    [InlineData("VBA", "Hello, VBA")]
    [InlineData("小村", "Hello, 小村")]
    public void Hello_WithName_ReturnsGreeting(string name, string expected)
    {
        var calc = new Calculator();

        Assert.Equal(expected, calc.Hello(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hello_EmptyOrWhiteSpace_ReturnsHello(string name)
    {
        var calc = new Calculator();

        Assert.Equal("Hello", calc.Hello(name));
    }

    [Fact]
    public void Calculator_ImplementsICalculator()
    {
        // VBA からは Dim calc As VbaTypedComSample.ICalculator で受ける前提（記事 8 章）
        Assert.IsAssignableFrom<ICalculator>(new Calculator());
    }

    [Fact]
    public void Calculator_HasPublicParameterlessConstructor()
    {
        // COM から New されるので、public な引数なしコンストラクターが必要（記事 4.3）
        var ctor = typeof(Calculator).GetConstructor(Type.EmptyTypes);

        Assert.NotNull(ctor);
        Assert.True(ctor.IsPublic);
    }

    [Fact]
    public void ComContract_GuidsAndInterfaceType_AreStable()
    {
        // COM では GUID が契約そのもの（記事 10.3）。軽率に変えると既存の VBA 参照や登録が壊れる
        var interfaceType = typeof(ICalculator);
        var classType = typeof(Calculator);

        Assert.Equal("2a1bbede-de6e-4c34-ad60-2e9e0e33e999", interfaceType.GUID.ToString());
        Assert.Equal("fad1c752-0bb6-4ddd-889f-fe446350847a", classType.GUID.ToString());

        // VBA 向けインターフェイスは InterfaceIsDual（記事 4.3）
        var interfaceTypeAttr = interfaceType.GetCustomAttributes(typeof(InterfaceTypeAttribute), false)
            .Cast<InterfaceTypeAttribute>()
            .Single();
        Assert.Equal(ComInterfaceType.InterfaceIsDual, interfaceTypeAttr.Value);

        // クラスは ClassInterfaceType.None（AutoDual に逃げない。記事 10.2）
        var classInterfaceAttr = classType.GetCustomAttributes(typeof(ClassInterfaceAttribute), false)
            .Cast<ClassInterfaceAttribute>()
            .Single();
        Assert.Equal(ClassInterfaceType.None, classInterfaceAttr.Value);
    }
}
