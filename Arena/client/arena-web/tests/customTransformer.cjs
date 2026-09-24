const { TsJestTransformer } = require('ts-jest');

class CustomTransformer extends TsJestTransformer {
  process(sourceText, sourcePath, transformOptions) {
    const transformed = sourceText.replace(/\bimport\.meta\.env\b/g, 'process.env');
    return super.process(transformed, sourcePath, transformOptions);
  }
}

module.exports = {
  createTransformer(tsJestConfig) {
    return new CustomTransformer(tsJestConfig);
  },
};
