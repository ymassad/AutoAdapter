using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class CompositeModuleProcessor : IModuleProcessor
    {
        private readonly IModuleProcessor[] processors;

        public CompositeModuleProcessor(params IModuleProcessor[] processors)
        {
            this.processors = processors;
        }

        public ChangesToModule ProcessModule(ModuleDefinition module)
        {
            return processors
                .Select(x => x.ProcessModule(module))
                .Aggregate(ChangesToModule.Empty(), ChangesToModule.Merge);
        }
    }
}