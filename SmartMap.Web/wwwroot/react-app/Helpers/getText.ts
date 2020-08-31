import { ITranslation } from '../service-model';

export function getText(key: string, translations: ITranslation[], fallback: string): string {
    let value = translations.find(t => t.key == key?.toLowerCase())?.value;
    return value == null ? fallback : value;
}