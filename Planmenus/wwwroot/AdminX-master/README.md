# AdminX

[![npm version](https://img.shields.io/npm/v/adminx.svg)](https://www.npmjs.com/package/adminx)

AdminX is feature-packed, responsive admin template based on [Bootstrap 4](http://getbootstrap.com/). It's currently under active development so if you have any idea on what you like to see in it, feel free to tell us.

Check out the Demo [here](https://movact.github.io/AdminX/index.html) if you want to have a look at the current progress.

### Documentation
As this template is still in very early stages of development a documentation is not yet available but the [Bootstrap documentation](http://getbootstrap.com/docs/4.0/getting-started/introduction/) may be a good first start for you to check out, as we try to use as many elements from it as possible.

### jQuery & vanilla JS available 🎉
As we are not using jQuery in a lot of projects we decided to maintain this theme with jQuery as well as vanilla JS. All our additional JavaScript is written without jQuery as a dependency. In [adminx.vanilla.js](dist/adminx.vanilla.js) we bundled [Bootstrap Native](https://github.com/thednp/bootstrap.native) so you can use all the Bootstrap components without jQuery. 👏 So we leave it up to you to use jQuery or not. :)

### Contribute
It's probably to early to contribute yet, but if want to contribute you can fork this repo and create a pull request.

When you downloaded the code install the dependencies:
```bash
npm install
```

You can watch the assets and autocompile by running:
```bash
npm run watch
```

We included a small locale development server with express. It serves the layout and will also rewrite the assets from using `/dev/` instead of the `/dist/` folder automatically.
```bash
npm run serve
```

If you're done and want to create a production build in the `/dist/` folder run
```bash
npm run production
```

### Credit
Credit is where credit's due: This admin template is inspired by the great [AdminLTE](https://adminlte.io/), which is why you might encounter similarity in terms of scope, but we do write all our own components and don't use any of AdminLTEs code. AdminLTE is also based on Bootstrap 3 and therefore created with LESS instead of SASS, which is our "go to" css preprocessor. We also try to go for a more minimal, clean and modern design.

### License
This project is licensed under [MIT](http://opensource.org/licenses/MIT).
