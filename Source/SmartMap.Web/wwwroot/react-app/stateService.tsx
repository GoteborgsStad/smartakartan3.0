declare const window: any;

interface IStateParameter {
    key: string;
    value: string;
}

export class StateService {
    public static get<T>(key: string): T | null {
        try {
            if (!window.sk.state) {
                return null;
            }

            const parameters = window.sk.state as IStateParameter[];

            // Don't use "parameters.find(p => p.key === key)" because IE cries about it? Change target?
            const entry = parameters.find(function (p) { return p.key === key });

            return entry !== undefined ? entry.value as any : null;
        } catch {
            return null;
        }
    }
}