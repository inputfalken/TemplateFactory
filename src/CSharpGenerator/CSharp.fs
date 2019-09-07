﻿namespace CSharpGenerator

open JsonParser
open CSharpGenerator.Types
open CSharpGenerator.Arguments
open System

module private stringValidators =
    let valueExists input =
       input
       |> Option.Some
       |> Option.filter (fun x -> not (System.String.IsNullOrWhiteSpace(x)))
       |> Option.map (fun x -> x.Trim())

type CSharp =
    static member CreateFile(input: string) =
        CSharp.CreateFile(input, new Settings())

    static member CreateFile(input: string, settings: Settings) =

        let rootObject = settings.RootObjectName
                         |> stringValidators.valueExists
                         |> Option.defaultValue "Root"

        let classPrefixExists = settings.ClassPrefix |> stringValidators.valueExists
        let classSuffixExists = settings.ClassSuffix |> stringValidators.valueExists

        let (classPrefix, classSuffix) = if classSuffixExists.IsNone && classPrefixExists.IsNone then (String.Empty, "Model")
                                         else (classPrefixExists |> Option.defaultValue String.Empty, classSuffixExists |> Option.defaultValue String.Empty)

        let unresolvedBaseType = BaseType.Object |> BaseType

        let rec baseType value: CSType Option =
            match value with
            | DateTime _ -> BaseType.DateTime |> BaseType |> Option.Some
            | Decimal _ -> BaseType.Decimal |> BaseType |> Option.Some
            | String _ -> BaseType.String |> BaseType |> Option.Some
            | Boolean _ -> BaseType.Boolean |> BaseType |> Option.Some
            | Guid _ -> BaseType.Guid |> BaseType |> Option.Some
            | Double _ -> BaseType.Double |> BaseType |> Option.Some
            | Object x -> generatedType x |> Option.Some
            | Array x -> stringifyArray x |> Option.Some
            | Null -> Option.None

        and stringifyArray (value: Value seq): CSType =
            if Seq.isEmpty value then unresolvedBaseType
            else
                 value
                 |> Seq.choose baseType
                 |> Seq.reduce (fun x y ->
                     let comparison = match x with
                                       | GeneratedType x1 ->
                                           match y with
                                           | GeneratedType x2 ->
                                               List.map2 (fun elem1 elem2 ->
                                                   if elem1 = elem2 then elem1
                                                   else
                                                       let (elem1Name, _) = elem1
                                                       let (elem2Name, _) = elem2
                                                       if elem1Name <> elem2Name then
                                                           raise (new Exception("Could not generate unresolved type when keys differ."))
                                                       (elem1Name, unresolvedBaseType |> ArrType)
                                               ) x1.Members x2.Members
                                               |> (fun x -> { Members = x; NamePrefix = classPrefix; NameSuffix = classSuffix })
                                               |> Option.Some
                                           | _ -> Option.None
                                       | _ -> Option.None
                     if comparison.IsSome then comparison.Value |> CSType.GeneratedType
                     else if x = y then y
                     else unresolvedBaseType
                 )
            |> CSType.ArrType

        and generatedType (properties: Property seq): CSType =
            properties
            |> Seq.map (fun x ->
                match baseType x.Value with
                | Some ``type`` -> (x.Key, ``type``)
                | None -> (x.Key, unresolvedBaseType)
            )
            |> Seq.toList
            |> (fun x -> { Members = x; NameSuffix = classSuffix; NamePrefix = classPrefix })
            |> CSType.GeneratedType

        let namespaceFormatter = settings.NameSpace
                                 |> stringValidators.valueExists
                                 |> Option.map (fun x -> sprintf "namespace %s { %s }" x)
                                 |> Option.defaultValue (sprintf "%s")

        let data = (input, settings.Casing |> CasingRule.fromString |> Option.defaultValue CasingRule.Pascal) ||> Json.parse
        baseType data
        |> Option.map (fun x ->
            match x with
            | GeneratedType x -> x.ClassDeclaration
            | ArrType x -> x.FormatArray
            | BaseType x -> x.FormatProperty
            <| rootObject
            |> namespaceFormatter
        )
        |> Option.defaultValue String.Empty
