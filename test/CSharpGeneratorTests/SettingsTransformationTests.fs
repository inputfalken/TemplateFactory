module CSharpGeneratorTests.SettingsTransformationTests

open CSharp.Types
open Common.Casing
open Generator
open Xunit
open Generator.Config


let defaultSettings =
    { RootName = "root"
      NameSpace = Option.None
      ClassSuffix = "model"
      ClassPrefix = System.String.Empty
      ClassCasing = Casing.Pascal
      PropertyCasing = Casing.Pascal }


[<Fact>]
let ``Empty object``() =
    let result = transformCSharpSettings (CSharpSettings())
    Assert.Equal(defaultSettings, result)

[<Theory>]
[<InlineData("Foobar")>]
[<InlineData("Foo")>]
[<InlineData("Bar")>]
[<InlineData("John")>]
[<InlineData("Doe")>]
let ``Setting namespace`` nameSpace =
    let result = transformCSharpSettings (CSharpSettings(NameSpace = nameSpace))
    Assert.Equal(Option.Some(nameSpace), result.NameSpace)

[<Fact>]
let ``Allow classPrefix to be empty if classSuffix is set``() =
    let result = transformCSharpSettings (CSharpSettings(ClassPrefix = System.String.Empty, ClassSuffix = "Foo"))
    Assert.Equal(System.String.Empty, result.ClassPrefix)

[<Fact>]
let ``Allow classSuffix to be empty if classPrefix is set``() =
    let result = transformCSharpSettings (CSharpSettings(ClassPrefix = "Foo", ClassSuffix = System.String.Empty))
    Assert.Equal(System.String.Empty, result.ClassSuffix)

[<Theory>]
[<InlineData(" ", " ")>]
[<InlineData("\t", "\t")>]
[<InlineData("", "")>]
[<InlineData(null, null)>]
[<InlineData(null, "")>]
[<InlineData("", null)>]
let ``Uses default settings if classSuffix or classPrefix is set to empty string or null`` classPrefix classSuffix =
    let result = transformCSharpSettings (CSharpSettings(ClassPrefix = classPrefix, ClassSuffix = classSuffix))
    Assert.Equal(defaultSettings, result)

[<Theory>]
[<InlineData(" ", " ")>]
[<InlineData("\t", "\t")>]
[<InlineData("", "")>]
[<InlineData(null, null)>]
[<InlineData(null, "")>]
[<InlineData("", null)>]
let ``Uses default settings if propertyCasing or classCasing is set to empty string or null`` classCasing propertyCasing =
    let result = transformCSharpSettings (CSharpSettings(ClassCasing = classCasing, PropertyCasing = propertyCasing))
    Assert.Equal(defaultSettings, result)
