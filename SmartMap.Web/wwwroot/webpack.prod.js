const merge = require('webpack-merge');
const common = require('./webpack.common.js');
const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CompressionPlugin = require('compression-webpack-plugin');

const output = {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].[contenthash].bundle.js'
}

const plugins = [
    // new webpack.ProvidePlugin({
    //     Popper: ['popper.js', 'default']
    // }),
    new MiniCssExtractPlugin({
        path: path.resolve(__dirname, 'dist'),
        filename: '[name].[contenthash].bundle.css'
    }),
    new CompressionPlugin(),
]

module.exports = merge(common, {
    mode: 'production',
    output,
    plugins,
});