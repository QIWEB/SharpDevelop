﻿using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ICSharpCode.CodeQualityAnalysis
{
    /// <summary>
    /// Reads neccesery information with Mono.Cecil to calculate code metrics
    /// </summary>
    public class MetricsReader
    {
        public Module MainModule { get; private set; } 

        public MetricsReader(string file)
        {
            this.ReadAssembly(file);
        }

        /// <summary>
        /// Opens a file as assembly and starts reading MainModule.
        /// </summary>
        /// <param name="file">A file which will be analyzed</param>
        private void ReadAssembly(string file)
        {
            var assembly = AssemblyDefinition.ReadAssembly(file);
            ReadModule(assembly.MainModule);
        }

        /// <summary>
        /// Reads a module from assembly.
        /// </summary>
        /// <param name="moduleDefinition">A module which contains information</param>
        private void ReadModule(ModuleDefinition moduleDefinition)
        {
            this.MainModule = new Module()
                                  {
                                      Name = moduleDefinition.Name
                                  };

            if (moduleDefinition.HasTypes)
                ReadTypes(MainModule, moduleDefinition.Types);
        }

        /// <summary>
        /// Reads types from module.
        /// </summary>
        /// <param name="module">A module where types will be added</param>
        /// <param name="types">A collection of types</param>
        private void ReadTypes(Module module, Collection<TypeDefinition> types)
        {
            // first add all types, because i will need find depend types

            AddTypes(module, types);

            ReadFromTypes(module, types);
        }

        /// <summary>
        /// Iterates through a collection of types and add them to the module.
        /// </summary>
        /// <param name="module">A module where types will be added</param>
        /// <param name="types">A collection of types</param>
        private void AddTypes(Module module, Collection<TypeDefinition> types)
        {
            foreach (TypeDefinition typeDefinition in types)
            {
                if (typeDefinition.Name != "<Module>")
                {
                    var type = CreateType(module, typeDefinition);

                    if (typeDefinition.HasNestedTypes)
                        AddNestedTypes(type, typeDefinition.NestedTypes);
                }
            }
        }

        /// <summary>
        /// Iterates through a collection of nested types and add them to the parent type.
        /// </summary>
        /// <param name="parentType">A type which is owner of nested types</param>
        /// <param name="types">A collection of nested types</param>
        private void AddNestedTypes(Type parentType, Collection<TypeDefinition> types)
        {
            foreach (TypeDefinition typeDefinition in types)
            {
                if (typeDefinition.Name != "<Module>")
                {
                    var type = CreateType(parentType.Namespace.Module, typeDefinition);

                    parentType.NestedTypes.Add(type);
                    type.Owner = parentType;

                    if (typeDefinition.HasNestedTypes)
                        AddNestedTypes(type, typeDefinition.NestedTypes);
                }
            }
        }

        /// <summary>
        /// Creates a type. If type exist in namespace which isn't created yet so it will be created.
        /// </summary>
        /// <param name="module">A module where type will be added</param>
        /// <param name="typeDefinition">TypeDefinition which will used to create a type.</param>
        /// <returns>A new type</returns>
        private Type CreateType(Module module, TypeDefinition typeDefinition)
        {
            var type = new Type()
                           {
                               Name = FormatTypeName(typeDefinition),
                               Owner = null
                           };

            // try find namespace
            var nsName = GetNamespaceName(typeDefinition);

            var ns = (from n in module.Namespaces
                      where n.Name == nsName
                      select n).SingleOrDefault();

            if (ns == null)
            {
                ns = new Namespace()
                         {
                             Name = nsName,
                             Module = module
                         };

                module.Namespaces.Add(ns);
            }

            type.Namespace = ns;
            ns.Types.Add(type);
            return type;
        }

        /// <summary>
        /// Reads fields, events, methods from a type.
        /// </summary>
        /// <param name="module">A module where are types located</param>
        /// <param name="types">A collection of types</param>
        private void ReadFromTypes(Module module, Collection<TypeDefinition> types)
        {
            foreach (TypeDefinition typeDefinition in types)
            {

                if (typeDefinition.Name != "<Module>")
                {
                    var type =
                        (from n in module.Namespaces
                         from t in n.Types
                         where (t.Name == FormatTypeName(typeDefinition))
                         select t).SingleOrDefault();


                    if (typeDefinition.HasFields)
                        ReadFields(type, typeDefinition.Fields);

                    if (typeDefinition.HasEvents)
                        ReadEvents(type, typeDefinition.Events);

                    if (typeDefinition.HasMethods)
                        ReadMethods(type, typeDefinition.Methods);

                    if (typeDefinition.HasNestedTypes)
                        ReadFromTypes(module, typeDefinition.NestedTypes);
                }
            }
        }

        /// <summary>
        /// Reads events and add them to the type.
        /// </summary>
        /// <param name="type">A type where events will be added</param>
        /// <param name="events">A collection of events</param>
        private void ReadEvents(Type type, Collection<EventDefinition> events)
        {
            foreach (var eventDefinition in events)
            {
                var e = new Event()
                            {
                                Name = eventDefinition.Name,
                                Owner = type
                            };

                type.Events.Add(e);

                var declaringType =
                    (from n in type.Namespace.Module.Namespaces
                     from t in n.Types
                     where t.Name == e.Name
                     select t).SingleOrDefault();

                e.EventType = declaringType;

                // Mono.Cecil threats Events as regular fields
                // so I have to find a field and set IsEvent to true

                var field =
                    (from n in type.Namespace.Module.Namespaces
                     from t in n.Types
                     from f in t.Fields
                     where f.Name == e.Name && f.Owner == e.Owner
                     select f).SingleOrDefault();

                if (field != null)
                    field.IsEvent = true;
            }
        }

        /// <summary>
        /// Reads fields and add them to the type.
        /// </summary>
        /// <param name="type">A type where fields will be added</param>
        /// <param name="fields">A collection of fields</param>
        private void ReadFields(Type type, Collection<FieldDefinition> fields)
        {
            foreach (FieldDefinition fieldDefinition in fields)
            {
                var field = new Field()
                                {
                                    Name = fieldDefinition.Name,
                                    IsEvent = false,
                                    Owner = type
                                };

                type.Fields.Add(field);

                var declaringType =
                        (from n in type.Namespace.Module.Namespaces
                         from t in n.Types
                         where t.Name == FormatTypeName(fieldDefinition.DeclaringType)
                         select t).SingleOrDefault();

                field.FieldType = declaringType;
            }
        }

        /// <summary>
        /// Extracts methods and add them to method list for type.
        /// </summary>
        /// <param name="type">A type where methods will be added</param>
        /// <param name="methods">A collection of methods</param>
        private void ReadMethods(Type type, Collection<MethodDefinition> methods)
        {
            foreach (MethodDefinition methodDefinition in methods)
            {
                var method = new Method
                {
                    Name = FormatMethodName(methodDefinition),
                    Owner = type,
                    IsConstructor = methodDefinition.IsConstructor
                };

                var declaringType =
                        (from n in type.Namespace.Module.Namespaces
                         from t in n.Types
                         where t.Name == FormatTypeName(methodDefinition.DeclaringType)
                         select t).SingleOrDefault();

                method.MethodType = declaringType;

                type.Methods.Add(method);
            }

            foreach (MethodDefinition methodDefinition in methods)
            {
                var method = (from m in type.Methods
                              where m.Name == FormatMethodName(methodDefinition)
                              select m).SingleOrDefault();

                if (methodDefinition.Body != null)
                {
                    ReadInstructions(method, methodDefinition, methodDefinition.Body.Instructions);
                }
            }
        }

        /// <summary>
        /// Reads method calls by extracting instructions.
        /// </summary>
        /// <param name="method">A method where information will be added</param>
        /// <param name="methodDefinition">A method definition with instructions</param>
        /// <param name="instructions">A collection of instructions</param>
        public void ReadInstructions(Method method, MethodDefinition methodDefinition,
            Collection<Instruction> instructions)
        {
            foreach (Instruction instruction in instructions)
            {
                var instr = ReadInstruction(instruction);
                
                if (instr is MethodDefinition)
                {
                    var md = instr as MethodDefinition;
                    var type = (from n in method.MethodType.Namespace.Module.Namespaces
                                from t in n.Types
                                where t.Name == FormatTypeName(md.DeclaringType) &&
                                n.Name == t.Namespace.Name
                                select t).SingleOrDefault();

                    method.TypeUses.Add(type);

                    var findTargetMethod = (from m in type.Methods
                                            where m.Name == FormatMethodName(md)
                                            select m).SingleOrDefault();

                    if (findTargetMethod != null && type == method.MethodType) 
                        method.MethodUses.Add(findTargetMethod);
                }

                if (instr is FieldDefinition)
                {
                    var fd = instr as FieldDefinition;
                    var field = (from f in method.MethodType.Fields
                                where f.Name == fd.Name
                                select f).SingleOrDefault();

                    if (field != null)
                        method.FieldUses.Add(field);
                }
            }
        }

        /// <summary>
        /// Reads instruction operand by recursive calling until non-instruction
        /// operand is found 
        /// </summary>
        /// <param name="instruction">An instruction with operand</param>
        /// <returns>An instruction operand</returns>
        public object ReadInstruction(Instruction instruction)
        {
            if (instruction.Operand == null)
                return null;
            
            var nextInstruction = instruction.Operand as Instruction;

            if (nextInstruction != null)
                return ReadInstruction(nextInstruction);
            
            return instruction.Operand;
        }

        /// <summary>
        /// Formats method name by adding parameters to it. If there are not any parameters
        /// only empty brackers will be added.
        /// </summary>
        /// <param name="methodDefinition">A method definition with parameters and name</param>
        /// <returns>A name with parameters</returns>
        public string FormatMethodName(MethodDefinition methodDefinition)
        {
            if (methodDefinition.HasParameters)
            {
                var builder = new StringBuilder();
                var enumerator = methodDefinition.Parameters.GetEnumerator();
                bool hasNext = enumerator.MoveNext();
                while (hasNext)
                {
                    builder.Append((enumerator.Current).ParameterType.FullName);
                    hasNext = enumerator.MoveNext();
                    if (hasNext)
                        builder.Append(", ");
                }

                return methodDefinition.Name + "(" + builder + ")";
            }

            return methodDefinition.Name + "()";
        }

        /// <summary>
        /// Formats a specific type name. If type is generic. Brackets <> will be added with proper names of parameters.
        /// </summary>
        /// <param name="typeDefinition">A type definition with declaring type and name</param>
        /// <returns>A type name</returns>
        public string FormatTypeName(TypeDefinition typeDefinition)
        {
            if (typeDefinition.IsNested && typeDefinition.DeclaringType != null)
            {
                return FormatTypeName(typeDefinition.DeclaringType) + "+" + typeDefinition.Name;
            }

            if (typeDefinition.HasGenericParameters)
            {
                var builder = new StringBuilder();
                var enumerator = typeDefinition.GenericParameters.GetEnumerator();
                bool hasNext = enumerator.MoveNext();
                while (hasNext)
                {
                    builder.Append((enumerator.Current).Name);
                    hasNext = enumerator.MoveNext();
                    if (hasNext)
                        builder.Append(",");
                }

                return StripGenericName(typeDefinition.Name) + "<" + builder + ">";
            }

            return typeDefinition.Name; 
        }

        /// <summary>
        /// Removes a number of generics parameters. Eg. `3 will be removed from end of name.
        /// </summary>
        /// <param name="name">A name with a number of generics parameters</param>
        /// <returns>A name without generics parameters</returns>
        private string StripGenericName(string name)
        {
            return name.IndexOf('`') != -1 ? name.Remove(name.IndexOf('`')) : name;
        }

        /// <summary>
        /// Gets a namespace name. If type is nested it looks recursively to parent type until finds his namespace.
        /// </summary>
        /// <param name="type">A type definition with namespace</param>
        /// <returns>A namespace</returns>
        private string GetNamespaceName(TypeDefinition type)
        {
            if (type.IsNested && type.DeclaringType != null)
                return GetNamespaceName(type.DeclaringType);
            
            if (!String.IsNullOrEmpty(type.Namespace))
                return type.Namespace;
            
            return "-";
        }
    }
}