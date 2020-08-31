const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

// https://webpack.js.org/configuration/mode/
// https://webpack.js.org/guides/production/

const entry = {
    'reactmain': './react-app/index.tsx'
}

const _module = {
    rules: [
        {
            test: /\.tsx?$/,
            use: 'ts-loader',
            exclude: /node_modules/,
        },
        {
            test: /\.s[ac]ss$/i,
            exclude: /node_modules/,
            use: [
                MiniCssExtractPlugin.loader,
                {
                    loader: 'css-loader',
                    options: {
                        sourceMap: true
                    }
                },
                'postcss-loader',
                {
                    // Compiles Sass to CSS
                    loader: 'sass-loader',
                    options: {
                        sourceMap: true
                    }
                }
            ],
        },
        {
            test: /\.(woff|woff2|eot|ttf|otf|svg|png|jpg|gif|ico)(\?v=[0-9]\.[0-9]\.[0-9])?$/,
            loader: 'file-loader'
        },
    ]
}

const resolve = {
    extensions: ['.tsx', '.ts', '.js'],
}

const plugins = [
    new CleanWebpackPlugin({
        // dry: true, // remove this once you verify it removes the correct files
        verbose: true,
    }),
]

module.exports = {
    entry,
    plugins,
    module: _module,
    resolve,
};
