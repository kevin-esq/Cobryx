import { en } from "./messages/en";
import { es, type DictionaryShape } from "./messages/es";
import { defaultLocale, type Locale } from "./config";

export type Dictionary = DictionaryShape;

const dictionaries: Record<Locale, Dictionary> = {
  es,
  en
};

export function getDictionary(locale: Locale): Dictionary {
  return dictionaries[locale] ?? dictionaries[defaultLocale];
}
