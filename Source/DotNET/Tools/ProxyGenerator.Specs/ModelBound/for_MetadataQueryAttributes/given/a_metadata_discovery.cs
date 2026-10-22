// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes.given;

public class a_metadata_discovery : Specification
{
    protected MetadataLoadContext _context;
    protected Assembly _assembly;
    private protected MetadataQueryAttributes _attributes;
    protected List<string> _openedPaths;
    protected List<PEReader> _images;

    void Establish()
    {
        var file = typeof(a_metadata_discovery).Assembly.Location;
        var paths = Directory.GetFiles(Path.GetDirectoryName(file)!, "*.dll")
            .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        _context = new MetadataLoadContext(new CanonicalAssemblyResolver(paths));
        _assembly = _context.LoadFromAssemblyPath(file);
        _openedPaths = [];
        _images = [];
        _attributes = new MetadataQueryAttributes(path =>
        {
            _openedPaths.Add(path);
            using var stream = File.OpenRead(path);
            var image = new PEReader(stream, PEStreamOptions.PrefetchMetadata);
            _images.Add(image);
            return image;
        });
    }

    void Destroy()
    {
        _attributes.Dispose();
        _context.Dispose();
    }

    protected Type MetadataType<T>() => _assembly.GetType(typeof(T).FullName!)!;
}
