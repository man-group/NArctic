# NArctic

This is a port of [Arctic](https://github.com/manahl/arctic/) to .net/mono

## Status

Currently it is possible to read and append to Arctic's ndarray store, but versioning is not supported.
So if you append data, old versions are not retained.

Most attention has been paid to reading data in this release, and test coverage is still insufficient.

## Performance

This port takes at least twice as long as the original python/numpy version to read dataframes. Strings are especially slow.

## Dependencies:

 * [NumCIL](https://github.com/bh107/bohrium/tree/master/bridge/NumCIL/NumCIL)
 * [Meta-Numerics](https://github.com/cureos/metanumerics)

## License

NArctic is licensed under the GNU LGPL v2.1.  A copy of which is included in [LICENSE](LICENSE)
